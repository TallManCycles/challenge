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
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GarminWebhookService> _logger;
    private readonly IGarminActivityProcessingService _activityProcessingService;
    private readonly IServiceScopeFactory _scopeFactory;

    public GarminWebhookService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<GarminWebhookService> logger,
        IGarminActivityProcessingService activityProcessingService,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _activityProcessingService = activityProcessingService;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> ProcessPingNotificationAsync(string webhookType, string payload)
    {
        try
        {
            // Store raw payload first
            var webhookPayload = new GarminWebhookPayload
            {
                WebhookType = ParseWebhookType(webhookType),
                RawPayload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.GarminWebhookPayloads.Add(webhookPayload);
            await _context.SaveChangesAsync();

            // Process ping notification to fetch actual data
            await ProcessPingPayloadAsync(payload, webhookPayload.Id);

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
            // Store raw payload first
            var webhookPayload = new GarminWebhookPayload
            {
                WebhookType = ParseWebhookType(webhookType),
                RawPayload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.GarminWebhookPayloads.Add(webhookPayload);
            await _context.SaveChangesAsync();

            // Process push notification directly (data is in payload)
            await ProcessPushPayloadAsync(payload, webhookPayload.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process push notification for type {WebhookType}", webhookType);
            return false;
        }
    }

    private async Task ProcessPingPayloadAsync(string payload, int payloadId)
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
                        await FetchAndProcessActivityDataAsync(callbackUrl, payloadId);
                    }
                }
            }
        }
    }

    private async Task FetchAndProcessActivityDataAsync(string callbackUrl, int payloadId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GarminOAuth");
            var response = await httpClient.GetAsync(callbackUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string activityData = await response.Content.ReadAsStringAsync();
                await ProcessActivityDataAsync(activityData, payloadId);
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

    private async Task ProcessPushPayloadAsync(string payload, int payloadId)
    {
        await ProcessActivityDataAsync(payload, payloadId);
    }

    private async Task ProcessActivityDataAsync(string activityData, int payloadId)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(activityData);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("activityDetails", out var activitiesElement))
            {
                foreach (var activityElement in activitiesElement.EnumerateArray())
                {
                    await ProcessSingleActivityAsync(activityElement, payloadId);
                }
            }
            
            // Mark payload as processed
            var webhookPayload = await _context.GarminWebhookPayloads.FindAsync(payloadId);
            if (webhookPayload != null)
            {
                webhookPayload.IsProcessed = true;
                webhookPayload.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity data for payload {PayloadId}", payloadId);
            
            // Mark payload as failed
            var webhookPayload = await _context.GarminWebhookPayloads.FindAsync(payloadId);
            if (webhookPayload != null)
            {
                webhookPayload.ProcessingError = ex.Message;
                webhookPayload.RetryCount = (webhookPayload.RetryCount ?? 0) + 1;
                webhookPayload.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, webhookPayload.RetryCount.Value));
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task ProcessSingleActivityAsync(JsonElement activityElement, int payloadId)
    {
        try
        {
            string summaryId = activityElement.GetProperty("summaryId").GetString() ?? "";
            
            // Check for duplicate
            bool exists = await _context.GarminActivities
                .AnyAsync(a => a.SummaryId == summaryId);
            
            if (exists)
            {
                _logger.LogInformation("Activity {SummaryId} already exists, skipping", summaryId);
                return;
            }

            // Extract user ID from various possible locations
             int userId = ExtractUserId(activityElement);
            if (userId == 0)
            {
                _logger.LogWarning("Could not extract userId from activity {SummaryId}", summaryId);
                return;
            }

            // Parse activity data
            var activity = ParseActivity(activityElement, userId);
            
            _context.GarminActivities.Add(activity);
            await _context.SaveChangesAsync();

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing single activity from payload {PayloadId}", payloadId);
            throw;
        }
    }

    private int ExtractUserId(JsonElement activityElement)
    {
        // Try userAccessToken first (from push notifications) - this would need to be mapped to actual user ID
        if (activityElement.TryGetProperty("userAccessToken", out var uatElement))
        {
            string userAccessToken = uatElement.GetString() ?? "";
            // TODO: Map userAccessToken to actual UserId from GarminOAuthTokens table
            return GetUserIdFromAccessToken(userAccessToken);
        }
        
        // Try userId (from ping notifications)
        if (activityElement.TryGetProperty("userId", out var userIdElement))
        {
            if (int.TryParse(userIdElement.GetString(), out int userId))
            {
                return userId;
            }
        }
        
        return 0;
    }

    private int GetUserIdFromAccessToken(string accessToken)
    {
        // Look up user by access token in GarminOAuthTokens table
        var token = _context.GarminOAuthTokens
            .FirstOrDefault(t => t.AccessToken == accessToken && t.IsAuthorized);
        
        return token?.UserId ?? 0;
    }

    private GarminActivity ParseActivity(JsonElement activityElement, int userId)
    {
        var activity = new GarminActivity
        {
            UserId = userId,
            SummaryId = activityElement.GetProperty("summaryId").GetString() ?? "",
            ResponseData = activityElement.GetRawText()
        };

        // Parse optional fields
        if (activityElement.TryGetProperty("activityId", out var activityIdElement))
            activity.ActivityId = activityIdElement.GetInt32().ToString();

        if (activityElement.TryGetProperty("activityType", out var activityTypeElement))
        {
            string activityTypeStr = activityTypeElement.GetString() ?? "";
            if (Enum.TryParse<GarminActivityType>(activityTypeStr, out var activityType))
            {
                activity.ActivityType = activityType;
            }
        }

        if (activityElement.TryGetProperty("startTimeInSeconds", out var startTimeElement))
        {
            activity.StartTime = DateTimeOffset.FromUnixTimeSeconds(startTimeElement.GetInt64()).DateTime;
        }

        if (activityElement.TryGetProperty("startTimeOffsetInSeconds", out var offsetElement))
            activity.StartTimeOffsetInSeconds = offsetElement.GetInt32();

        if (activityElement.TryGetProperty("durationInSeconds", out var durationElement))
            activity.DurationInSeconds = durationElement.GetInt32();

        if (activityElement.TryGetProperty("distanceInMeters", out var distanceElement))
            activity.DistanceInMeters = distanceElement.GetDouble();

        if (activityElement.TryGetProperty("totalElevationGainInMeters", out var elevGainElement))
            activity.TotalElevationGainInMeters = elevGainElement.GetDouble();

        if (activityElement.TryGetProperty("totalElevationLossInMeters", out var elevLossElement))
            activity.TotalElevationLossInMeters = elevLossElement.GetDouble();

        if (activityElement.TryGetProperty("activeKilocalories", out var caloriesElement))
            activity.ActiveKilocalories = caloriesElement.GetInt32();

        if (activityElement.TryGetProperty("deviceName", out var deviceElement))
            activity.DeviceName = deviceElement.GetString();

        if (activityElement.TryGetProperty("manual", out var manualElement))
            activity.IsManual = manualElement.GetBoolean();

        if (activityElement.TryGetProperty("isWebUpload", out var webUploadElement))
            activity.IsWebUpload = webUploadElement.GetBoolean();

        return activity;
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
        var unprocessedPayloads = await _context.GarminWebhookPayloads
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
                await ProcessActivityDataAsync(payload.RawPayload, payload.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reprocess payload {PayloadId}", payload.Id);
            }
        }
    }
}