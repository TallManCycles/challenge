using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using backend.Data;
using backend.Models;

namespace backend.Services;

public interface IGarminWebhookService
{
    Task<bool> ProcessPingNotificationAsync(string webhookType, string payload);
    Task<bool> ProcessPushNotificationAsync(string webhookType, string payload);
    Task ProcessStoredPayloadsAsync();
}

public class GarminWebhookService : IGarminWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GarminWebhookService> _logger;
    private readonly IGarminActivityProcessingService _activityProcessingService;
    private readonly IServiceScopeFactory _scopeFactory;

    public GarminWebhookService(
        IHttpClientFactory httpClientFactory,
        ILogger<GarminWebhookService> logger,
        IGarminActivityProcessingService activityProcessingService,
        IServiceScopeFactory scopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _activityProcessingService = activityProcessingService;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> ProcessPingNotificationAsync(string webhookType, string payload)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Store raw payload first
            var webhookPayload = new GarminWebhookPayload
            {
                WebhookType = ParseWebhookType(webhookType),
                RawPayload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            context.GarminWebhookPayloads.Add(webhookPayload);
            await context.SaveChangesAsync();

            // Process ping notification to fetch actual data
            await ProcessPingPayloadAsync(payload, webhookPayload.Id, scope.ServiceProvider);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ping notification for type {WebhookType}", webhookType);
            return false;
        }
    }

    public async Task<bool> ProcessPushNotificationAsync(string webhookType, string payload)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Store raw payload first
            var webhookPayload = new GarminWebhookPayload
            {
                WebhookType = ParseWebhookType(webhookType),
                RawPayload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            context.GarminWebhookPayloads.Add(webhookPayload);
            await context.SaveChangesAsync();

            // Process push notification directly (data is in payload)
            await ProcessPushPayloadAsync(payload, webhookPayload.Id, scope.ServiceProvider);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process push notification for type {WebhookType}", webhookType);
            return false;
        }
    }

    private async Task ProcessPingPayloadAsync(string payload, int payloadId, IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return; // Nothing to process
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(payload);
            var root = jsonDoc.RootElement;

            foreach (var summaryType in root.EnumerateObject())
            {
                if (summaryType.Name == "activities")
                {
                    foreach (var activity in summaryType.Value.EnumerateArray())
                    {
                        if (activity.TryGetProperty("callbackURL", out var callbackUrlElement))
                        {
                            string callbackUrl = callbackUrlElement.GetString() ?? "";
                            await FetchAndProcessActivityDataAsync(callbackUrl, payloadId, serviceProvider);
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in ping payload {PayloadId}", payloadId);
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var webhookPayload = await context.GarminWebhookPayloads.FindAsync(payloadId);
            if (webhookPayload != null)
            {
                webhookPayload.ProcessingError = $"Invalid JSON in ping payload: {ex.Message}";
                await context.SaveChangesAsync();
            }
        }
    }

    private async Task FetchAndProcessActivityDataAsync(string callbackUrl, int payloadId, IServiceProvider serviceProvider)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GarminOAuth");
            var response = await httpClient.GetAsync(callbackUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string activityData = await response.Content.ReadAsStringAsync();
                await ProcessActivityDataAsync(activityData, payloadId, serviceProvider);
            }
            else
            {
                _logger.LogWarning("Failed to fetch activity data from callback URL: {CallbackUrl}, Status: {StatusCode}", 
                    callbackUrl, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity data from callback URL: {CallbackUrl}", callbackUrl);
        }
    }

    private async Task ProcessPushPayloadAsync(string payload, int payloadId, IServiceProvider serviceProvider)
    {
        await ProcessActivityDataAsync(payload, payloadId, serviceProvider);
    }

    private async Task ProcessActivityDataAsync(string activityData, int payloadId, IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            using var jsonDoc = JsonDocument.Parse(activityData);
            var root = jsonDoc.RootElement;
            bool allActivitiesProcessed = true;

            if (root.TryGetProperty("activityDetails", out var activitiesElement))
            {
                foreach (var activityElement in activitiesElement.EnumerateArray())
                {
                    bool activityProcessed = await ProcessSingleActivityAsync(activityElement, payloadId, serviceProvider);
                    if (!activityProcessed)
                    {
                        allActivitiesProcessed = false;
                    }
                }
            }
            
            // Mark payload as processed only if all activities were processed successfully
            var webhookPayload = await context.GarminWebhookPayloads.FindAsync(payloadId);
            if (webhookPayload != null)
            {
                webhookPayload.IsProcessed = allActivitiesProcessed;
                if (allActivitiesProcessed)
                {
                    webhookPayload.ProcessedAt = DateTime.UtcNow;
                }
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity data for payload {PayloadId}", payloadId);
            
            // Mark payload as failed
            var webhookPayload = await context.GarminWebhookPayloads.FindAsync(payloadId);
            if (webhookPayload != null)
            {
                webhookPayload.ProcessingError = $"Error processing activity data: {ex.Message}";
                webhookPayload.RetryCount = (webhookPayload.RetryCount ?? 0) + 1;
                webhookPayload.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, webhookPayload.RetryCount.Value));
                await context.SaveChangesAsync();
            }
        }
    }

    private async Task<bool> ProcessSingleActivityAsync(JsonElement activityElement, int payloadId, IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            string summaryId = activityElement.GetProperty("summaryId").GetString() ?? "";
            
            // Check for duplicate
            bool exists = await context.GarminActivities
                .AnyAsync(a => a.SummaryId == summaryId);
            
            if (exists)
            {
                _logger.LogInformation("Activity {SummaryId} already exists, skipping", summaryId);
                return true; // Duplicate is considered successful
            }

            // Extract user ID from various possible locations
             int userId = await ExtractUserIdAsync(activityElement, serviceProvider);
            if (userId == 0)
            {
                _logger.LogWarning("Could not extract userId from activity {SummaryId}", summaryId);
                return false; // Failed to process due to missing user
            }

            // Parse activity data
            var activity = ParseActivity(activityElement, userId);
            
            context.GarminActivities.Add(activity);
            await context.SaveChangesAsync();

            // Process for challenges asynchronously
            //_ = Task.Run(async () => await _activityProcessingService.ProcessActivityForChallengesAsync(activity.Id));
            
            _ = Task.Run(async () =>
            {
                // Create a new scope for this background task.
                using var scope = _scopeFactory.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<GarminWebhookService>>();
                try
                {
                    // Resolve services from the new scope.
                    var activityProcessingService = scope.ServiceProvider.GetRequiredService<IGarminActivityProcessingService>();
                    await activityProcessingService.ProcessActivityForChallengesAsync(activity.Id);
                }
                catch (Exception ex)
                {
                    // Now we can safely log any exceptions from the background task.
                    logger.LogError(ex, "Background challenge processing failed for activity {ActivityId}", activity.Id);
                }
            });

            _logger.LogInformation("Successfully processed activity {SummaryId} for user {UserId}", summaryId, userId);
            return true; // Successfully processed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing single activity from payload {PayloadId}", payloadId);
            return false; // Failed to process due to exception
        }
    }

    private async Task<int> ExtractUserIdAsync(JsonElement activityElement, IServiceProvider serviceProvider)
    {
        // Try userAccessToken first (from push notifications) - this would need to be mapped to actual user ID
        if (activityElement.TryGetProperty("userAccessToken", out var uatElement))
        {
            string userAccessToken = uatElement.GetString() ?? "";
            return await GetUserIdFromAccessTokenAsync(userAccessToken, serviceProvider);
        }
        
        // Try userId (from activity details) - this is a Garmin UUID string
        if (activityElement.TryGetProperty("userId", out var userIdElement))
        {
            string garminUserId = userIdElement.GetString() ?? "";
            return await GetUserIdFromGarminUserIdAsync(garminUserId, serviceProvider);
        }
        
        return 0;
    }

    private async Task<int> GetUserIdFromAccessTokenAsync(string accessToken, IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Look up user by access token in GarminOAuthTokens table
        var token = await context.GarminOAuthTokens
            .FirstOrDefaultAsync(t => t.AccessToken == accessToken && t.IsAuthorized);
        
        return token?.UserId ?? 0;
    }

    private async Task<int> GetUserIdFromGarminUserIdAsync(string garminUserId, IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Look up user by Garmin User ID in Users table
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.GarminUserId == garminUserId);
        
        return user?.Id ?? 0;
    }

    private GarminActivity ParseActivity(JsonElement activityElement, int userId)
    {
        var activity = new GarminActivity
        {
            UserId = userId,
            SummaryId = activityElement.GetProperty("summaryId").GetString() ?? "",
            ResponseData = activityElement.GetRawText()
        };

        // Parse activityId from root level
        if (activityElement.TryGetProperty("activityId", out var activityIdElement))
            activity.ActivityId = activityIdElement.GetDecimal().ToString(CultureInfo.CurrentCulture);

        // Most fields are in the summary object
        if (activityElement.TryGetProperty("summary", out var summaryElement))
        {
            if (summaryElement.TryGetProperty("activityType", out var activityTypeElement))
            {
                string activityTypeStr = activityTypeElement.GetString() ?? "";
                if (Enum.TryParse<GarminActivityType>(activityTypeStr, out var activityType))
                {
                    activity.ActivityType = activityType;
                }
            }

            if (summaryElement.TryGetProperty("activityName", out var activityNameElement))
            {
                activity.ActivityName = activityNameElement.GetString();
            }

            if (summaryElement.TryGetProperty("startTimeInSeconds", out var startTimeElement))
            {
                activity.StartTime = DateTimeOffset.FromUnixTimeSeconds(startTimeElement.GetInt64()).DateTime;
            }

            if (summaryElement.TryGetProperty("startTimeOffsetInSeconds", out var offsetElement))
                activity.StartTimeOffsetInSeconds = offsetElement.GetInt32();

            if (summaryElement.TryGetProperty("durationInSeconds", out var durationElement))
                activity.DurationInSeconds = durationElement.GetInt32();

            if (summaryElement.TryGetProperty("deviceName", out var deviceElement))
                activity.DeviceName = deviceElement.GetString();

            if (summaryElement.TryGetProperty("manual", out var manualElement))
                activity.IsManual = manualElement.GetBoolean();

            if (summaryElement.TryGetProperty("isWebUpload", out var webUploadElement))
                activity.IsWebUpload = webUploadElement.GetBoolean();
            
            if (summaryElement.TryGetProperty("activeKilocalories", out var caloriesElement) && caloriesElement.ValueKind == JsonValueKind.Number)
                activity.ActiveKilocalories = caloriesElement.GetInt32();
        }

        // Process samples array for distance and elevation data
        if (activityElement.TryGetProperty("samples", out var samplesElement) && samplesElement.ValueKind == JsonValueKind.Array)
        {
            ProcessSamples(samplesElement, activity);
        }

        return activity;
    }

    private void ProcessSamples(JsonElement samplesElement, GarminActivity activity)
    {
        double maxDistance = 0;
        double minElevation = double.MaxValue;
        double maxElevation = double.MinValue;
        double totalElevationGain = 0;
        double totalElevationLoss = 0;
        double? previousElevation = null;

        foreach (var sample in samplesElement.EnumerateArray())
        {
            // Get maximum total distance (cumulative distance)
            if (sample.TryGetProperty("totalDistanceInMeters", out var distanceElement))
            {
                double distance = distanceElement.GetDouble();
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            // Process elevation for gain/loss calculations
            if (sample.TryGetProperty("elevationInMeters", out var elevationElement))
            {
                double elevation = elevationElement.GetDouble();
                
                // Track min/max elevation
                if (elevation < minElevation) minElevation = elevation;
                if (elevation > maxElevation) maxElevation = elevation;

                // Calculate elevation gain/loss
                if (previousElevation.HasValue)
                {
                    double elevationChange = elevation - previousElevation.Value;
                    if (elevationChange > 0)
                    {
                        totalElevationGain += elevationChange;
                    }
                    else if (elevationChange < 0)
                    {
                        totalElevationLoss += Math.Abs(elevationChange);
                    }
                }
                previousElevation = elevation;
            }
        }

        // Set the calculated values
        activity.DistanceInMeters = maxDistance > 0 ? maxDistance : null;
        activity.TotalElevationGainInMeters = totalElevationGain > 0 ? totalElevationGain : null;
        activity.TotalElevationLossInMeters = totalElevationLoss > 0 ? totalElevationLoss : null;
    }

    private GarminWebhookType ParseWebhookType(string webhookType)
    {
        return webhookType.ToLowerInvariant() switch
        {
            "activities" => GarminWebhookType.Activities,
            "activitydetails" => GarminWebhookType.ActivityDetails,
            "activityfiles" => GarminWebhookType.ActivityFiles,
            "manuallyupdatedactivities" => GarminWebhookType.ManuallyUpdatedActivities,
            "moveiqactivities" => GarminWebhookType.MoveIQActivities,
            _ => GarminWebhookType.Activities
        };
    }

    public async Task ProcessStoredPayloadsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var unprocessedPayloads = await context.GarminWebhookPayloads
            .Where(p => !p.IsProcessed && 
                       (p.NextRetryAt == null || p.NextRetryAt <= DateTime.UtcNow) &&
                       (p.RetryCount ?? 0) < 5)
            .OrderBy(p => p.ReceivedAt)
            .Take(10)
            .ToListAsync();

        foreach (var payload in unprocessedPayloads)
        {
            try
            {
                await ProcessActivityDataAsync(payload.RawPayload, payload.Id, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reprocess payload {PayloadId}", payload.Id);
                var errorPayload = await context.GarminWebhookPayloads.FindAsync(payload.Id);
                if (errorPayload != null)
                {
                    errorPayload.ProcessingError = $"Failed to reprocess payload: {ex.Message}";
                    errorPayload.RetryCount = (errorPayload.RetryCount ?? 0) + 1;
                    errorPayload.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, errorPayload.RetryCount.Value));
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
