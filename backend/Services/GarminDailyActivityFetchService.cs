using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using backend.Data;
using backend.Models;

namespace backend.Services;

public interface IGarminDailyActivityFetchService
{
    Task FetchActivitiesForAllUsersAsync();
    Task FetchActivitiesForUserAsync(int userId);
}

public class GarminDailyActivityFetchService : IGarminDailyActivityFetchService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileLoggingService _logger;
    private readonly IGarminActivityProcessingService _activityProcessingService;
    private readonly GarminOAuthConfig _config;

    public GarminDailyActivityFetchService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IFileLoggingService logger,
        IGarminActivityProcessingService activityProcessingService,
        IOptions<GarminOAuthConfig> config)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _activityProcessingService = activityProcessingService;
        _config = config.Value;
    }

    public async Task FetchActivitiesForAllUsersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await _logger.LogInfoAsync("Starting daily activity fetch for all users");

            // Get all users with valid Garmin OAuth tokens
             var authorizedUsers = await context.GarminOAuthTokens
                .Include(t => t.User)
                .Where(t => t.IsAuthorized && t.User != null)
                .Select(t => t.UserId)
                .Distinct()
                .ToListAsync();

            await _logger.LogInfoAsync($"Found {authorizedUsers.Count} users with authorized Garmin connections");

            // Process users concurrently but with some throttling to avoid overwhelming Garmin API
            var semaphore = new SemaphoreSlim(5, 5); // Limit to 5 concurrent requests
            var tasks = authorizedUsers.Select(async userId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await FetchActivitiesForUserAsync(userId);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            await _logger.LogInfoAsync("Completed daily activity fetch for all users");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error during daily activity fetch for all users", ex, "GarminDailyActivityFetchService");
        }
    }

    public async Task FetchActivitiesForUserAsync(int userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await _logger.LogInfoAsync($"Fetching activities for user {userId}");

            // Get user's valid OAuth token
            var token = await context.GarminOAuthTokens
                .Where(t => t.UserId == userId && t.IsAuthorized)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                await _logger.LogWarningAsync($"No valid OAuth token found for user {userId}");
                return;
            }

            // Check for required tokens
            if (string.IsNullOrEmpty(token.AccessToken) || string.IsNullOrEmpty(token.AccessTokenSecret))
            {
                await _logger.LogWarningAsync($"Invalid OAuth tokens for user {userId}");
                return;
            }

            // Make 3 separate calls, one for each of the past 3 days (due to 24-hour API limit)
            int totalProcessedCount = 0;
            int totalSkippedCount = 0;
            var now = DateTime.UtcNow;

            for (int dayOffset = 1; dayOffset <= 3; dayOffset++)
            {
                var dayStart = now.Date.AddDays(-dayOffset);
                var dayEnd = dayStart.AddDays(1);
                
                // Explicitly specify UTC time zone (TimeSpan.Zero) to ensure consistent time handling
                // This prevents issues with server local time zone affecting the Unix timestamp calculation
                var uploadStartTimeInSeconds = new DateTimeOffset(dayStart, TimeSpan.Zero).ToUnixTimeSeconds();
                var uploadEndTimeInSeconds = new DateTimeOffset(dayEnd, TimeSpan.Zero).ToUnixTimeSeconds();

                await _logger.LogInfoAsync($"Fetching activities for user {userId} for day {dayOffset} ago ({dayStart} to {dayEnd})");

                // Fetch activities for this specific day
                var activities = await FetchActivitiesFromGarminApiAsync(
                    token.AccessToken, 
                    token.AccessTokenSecret, 
                    uploadStartTimeInSeconds, 
                    uploadEndTimeInSeconds);

                if (activities == null || activities.Count == 0)
                {
                    await _logger.LogInfoAsync($"No activities found for user {userId} on day {dayOffset}");
                    continue;
                }

                await _logger.LogInfoAsync($"Found {activities.Count} activities for user {userId} on day {dayOffset}");

                // Process each activity for this day
                int dayProcessedCount = 0;
                int daySkippedCount = 0;

                foreach (var activityData in activities)
                {
                    try
                    {
                        var summaryId = activityData.GetProperty("summaryId").GetString();
                        
                        // Check if activity already exists
                        var existingActivity = await context.GarminActivities
                            .AnyAsync(a => a.SummaryId == summaryId);

                        if (existingActivity)
                        {
                            daySkippedCount++;
                            continue;
                        }

                        // Parse and save the activity
                        var activity = ParseActivity(activityData, userId);
                        context.GarminActivities.Add(activity);
                        await context.SaveChangesAsync();

                        // Process for challenges
                        await _activityProcessingService.ProcessActivityForChallengesAsync(activity.Id);
                        
                        dayProcessedCount++;
                        await _logger.LogInfoAsync($"Processed activity {summaryId} for user {userId}");
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogErrorAsync("Error processing individual activity for user {UserId}", ex, "GarminDailyActivityFetchService");
                    }
                }

                totalProcessedCount += dayProcessedCount;
                totalSkippedCount += daySkippedCount;

                // Add a small delay between API calls to be respectful to Garmin's rate limits
                if (dayOffset < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            await _logger.LogInfoAsync($"Completed 3-day activity fetch for user {userId}: {totalProcessedCount} processed, {totalSkippedCount} skipped");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error fetching activities for user {UserId}", ex, "GarminDailyActivityFetchService");
        }
    }

    private async Task<List<JsonElement>?> FetchActivitiesFromGarminApiAsync(
        string accessToken, 
        string accessTokenSecret, 
        long uploadStartTimeInSeconds, 
        long uploadEndTimeInSeconds)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GarminOAuth");
            
            // Build the base API URL for activities
            var baseApiUrl = "https://apis.garmin.com/wellness-api/rest/activities";
            
            // Create parameters dictionary for both OAuth and query parameters
            var allParams = new Dictionary<string, string>
            {
                // OAuth parameters
                { "oauth_consumer_key", _config.ConsumerKey },
                { "oauth_token", accessToken },
                { "oauth_nonce", GenerateNonce() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimestamp() },
                { "oauth_version", "1.0" },
                
                // Query parameters
                { "uploadStartTimeInSeconds", uploadStartTimeInSeconds.ToString() },
                { "uploadEndTimeInSeconds", uploadEndTimeInSeconds.ToString() }
            };

            // Extract only OAuth parameters for the Authorization header
            var oauthParams = allParams.Where(p => p.Key.StartsWith("oauth_"))
                                      .ToDictionary(p => p.Key, p => p.Value);

            // Generate signature using all parameters
            string signature = GenerateSignature("GET", baseApiUrl, allParams, _config.ConsumerSecret, accessTokenSecret);
            oauthParams.Add("oauth_signature", signature);

            // Create Authorization header with only OAuth parameters
            string authHeader = GenerateAuthorizationHeader(oauthParams);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

            // Build the full URL with query parameters
            var queryParams = allParams.Where(p => !p.Key.StartsWith("oauth_"))
                                      .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
            var fullApiUrl = $"{baseApiUrl}?{string.Join("&", queryParams)}";

            // Log the request details for debugging
            await _logger.LogInfoAsync($"Making Garmin API request to: {fullApiUrl}");
            await _logger.LogInfoAsync($"Authorization header: {authHeader}");

            var response = await httpClient.GetAsync(fullApiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                await _logger.LogWarningAsync($"Garmin API request failed with status {response.StatusCode}: {response.ReasonPhrase}");
                await _logger.LogWarningAsync($"Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                await _logger.LogWarningAsync($"Response content: {responseContent}");
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new List<JsonElement>();
            }

            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var activities = new List<JsonElement>();
            
            // The response should be a JSON array of activity summaries
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var activity in jsonDoc.RootElement.EnumerateArray())
                {
                    activities.Add(activity.Clone());
                }
            }

            return activities;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error calling Garmin Activities API", ex, "GarminDailyActivityFetchService");
            return null;
        }
    }

    private GarminActivity ParseActivity(JsonElement activityElement, int userId)
    {
        var activity = new GarminActivity
        {
            UserId = userId,
            SummaryId = activityElement.GetProperty("summaryId").GetString() ?? "",
            ResponseData = activityElement.GetRawText()
        };

        // Parse activityId
        if (activityElement.TryGetProperty("activityId", out var activityIdElement))
            activity.ActivityId = activityIdElement.GetDecimal().ToString();

        // Parse activityType
        if (activityElement.TryGetProperty("activityType", out var activityTypeElement))
        {
            string activityTypeStr = activityTypeElement.GetString() ?? "";
            if (Enum.TryParse<GarminActivityType>(activityTypeStr, out var activityType))
            {
                activity.ActivityType = activityType;
            }
        }

        // Parse startTime
        if (activityElement.TryGetProperty("startTimeInSeconds", out var startTimeElement))
        {
            activity.StartTime = DateTimeOffset.FromUnixTimeSeconds(startTimeElement.GetInt64()).DateTime;
        }

        // Parse other fields
        if (activityElement.TryGetProperty("startTimeOffsetInSeconds", out var offsetElement))
            activity.StartTimeOffsetInSeconds = offsetElement.GetInt32();

        if (activityElement.TryGetProperty("durationInSeconds", out var durationElement))
            activity.DurationInSeconds = durationElement.GetInt32();

        if (activityElement.TryGetProperty("distanceInMeters", out var distanceElement))
            activity.DistanceInMeters = distanceElement.GetDouble();

        if (activityElement.TryGetProperty("deviceName", out var deviceElement))
            activity.DeviceName = deviceElement.GetString();

        if (activityElement.TryGetProperty("manual", out var manualElement))
            activity.IsManual = manualElement.GetBoolean();

        if (activityElement.TryGetProperty("isWebUpload", out var webUploadElement))
            activity.IsWebUpload = webUploadElement.GetBoolean();

        if (activityElement.TryGetProperty("activeKilocalories", out var caloriesElement) && caloriesElement.ValueKind == JsonValueKind.Number)
            activity.ActiveKilocalories = caloriesElement.GetInt32();

        if (activityElement.TryGetProperty("totalElevationGainInMeters", out var elevationGainElement))
            activity.TotalElevationGainInMeters = elevationGainElement.GetDouble();

        if (activityElement.TryGetProperty("totalElevationLossInMeters", out var elevationLossElement))
            activity.TotalElevationLossInMeters = elevationLossElement.GetDouble();

        return activity;
    }

    private static string GenerateNonce()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        byte[] bytes = new byte[16];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private static string GenerateTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    private static string GenerateSignature(string method, string url, Dictionary<string, string> parameters, 
        string consumerSecret, string tokenSecret)
    {
        // Create a sorted list of all parameters by key
        var sortedParams = parameters.OrderBy(p => p.Key).ToList();
        
        // Create the parameter string with proper encoding
        var paramString = string.Join("&", sortedParams.Select(p => 
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        
        // Create the signature base string according to OAuth 1.0a spec
        string baseString = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";
        
        // Create the signing key
        string signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";
        
        // Generate the HMAC-SHA1 signature
        using var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.ASCII.GetBytes(signingKey));
        byte[] hashBytes = hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(baseString));
        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateAuthorizationHeader(Dictionary<string, string> parameters)
    {
        var headerParams = parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\"");
        return $"OAuth {string.Join(", ", headerParams)}";
    }
}