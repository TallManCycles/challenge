# Garmin Activity Webhook Implementation Guide

## Overview
This guide extends the Garmin OAuth implementation to support real-time activity webhooks using both Ping and Push services. The system will store all Garmin activities, process them for challenges, and handle duplicates appropriately.

## Architecture Overview

```
Garmin -> Webhook Endpoint -> Raw Payload Storage -> Activity Processing -> Challenge Updates
```

## Database Schema Extensions

### 1. Additional Models

```csharp
// Models/GarminActivity.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourApp.Models
{
    public class GarminActivity
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string SummaryId { get; set; } = string.Empty;
        
        public string? ActivityId { get; set; }
        
        [Required]
        public GarminActivityType ActivityType { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        public int StartTimeOffsetInSeconds { get; set; }
        
        [Required]
        public int DurationInSeconds { get; set; }
        
        public double? DistanceInMeters { get; set; }
        
        public double? TotalElevationGainInMeters { get; set; }
        
        public double? TotalElevationLossInMeters { get; set; }
        
        public int? ActiveKilocalories { get; set; }
        
        public string? DeviceName { get; set; }
        
        public bool IsManual { get; set; }
        
        public bool IsWebUpload { get; set; }
        
        [Required]
        [Column(TypeName = "jsonb")]
        public string ResponseData { get; set; } = string.Empty;
        
        [Required]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedAt { get; set; }
        
        public bool IsProcessed { get; set; } = false;
        
        public string? ProcessingError { get; set; }
    }

    public enum GarminActivityType
    {
        RUNNING,
        INDOOR_RUNNING,
        OBSTACLE_RUN,
        STREET_RUNNING,
        TRACK_RUNNING,
        TRAIL_RUNNING,
        TREADMILL_RUNNING,
        ULTRA_RUN,
        VIRTUAL_RUN,
        CYCLING,
        BMX,
        CYCLOCROSS,
        DOWNHILL_BIKING,
        E_BIKE_FITNESS,
        E_BIKE_MOUNTAIN,
        E_ENDURO_MTB,
        ENDURO_MTB,
        GRAVEL_CYCLING,
        INDOOR_CYCLING,
        MOUNTAIN_BIKING,
        RECUMBENT_CYCLING,
        ROAD_BIKING,
        TRACK_CYCLING,
        VIRTUAL_RIDE,
        HANDCYCLING,
        INDOOR_HANDCYCLING,
        FITNESS_EQUIPMENT,
        BOULDERING,
        ELLIPTICAL,
        INDOOR_CARDIO,
        HIIT,
        INDOOR_CLIMBING,
        INDOOR_ROWING,
        MOBILITY,
        PILATES,
        STAIR_CLIMBING,
        STRENGTH_TRAINING,
        YOGA,
        MEDITATION,
        SWIMMING,
        LAP_SWIMMING,
        OPEN_WATER_SWIMMING,
        WALKING,
        CASUAL_WALKING,
        SPEED_WALKING,
        HIKING,
        RUCKING,
        WINTER_SPORTS,
        BACKCOUNTRY_SNOWBOARDING,
        BACKCOUNTRY_SKIING,
        CROSS_COUNTRY_SKIING_WS,
        RESORT_SKIING,
        SNOWBOARDING_WS,
        RESORT_SKIING_SNOWBOARDING_WS,
        SKATE_SKIING_WS,
        SKATING_WS,
        SNOW_SHOE_WS,
        SNOWMOBILING_WS,
        WATER_SPORTS,
        BOATING_V2,
        BOATING,
        FISHING_V2,
        FISHING,
        KAYAKING_V2,
        KAYAKING,
        KITEBOARDING_V2,
        KITEBOARDING,
        OFFSHORE_GRINDING_V2,
        OFFSHORE_GRINDING,
        ONSHORE_GRINDING_V2,
        ONSHORE_GRINDING,
        PADDLING_V2,
        PADDLING,
        ROWING_V2,
        ROWING,
        SAILING_V2,
        SAILING,
        SNORKELING,
        STAND_UP_PADDLEBOARDING_V2,
        STAND_UP_PADDLEBOARDING,
        SURFING_V2,
        SURFING,
        WAKEBOARDING_V2,
        WAKEBOARDING,
        WATERSKIING,
        WHITEWATER_RAFTING_V2,
        WHITEWATER_RAFTING,
        WINDSURFING_V2,
        WINDSURFING,
        TRANSITION_V2,
        BIKE_TO_RUN_TRANSITION_V2,
        BIKE_TO_RUN_TRANSITION,
        RUN_TO_BIKE_TRANSITION_V2,
        RUN_TO_BIKE_TRANSITION,
        SWIM_TO_BIKE_TRANSITION_V2,
        SWIM_TO_BIKE_TRANSITION,
        TEAM_SPORTS,
        AMERICAN_FOOTBALL,
        BASEBALL,
        BASKETBALL,
        CRICKET,
        FIELD_HOCKEY,
        ICE_HOCKEY,
        LACROSSE,
        RUGBY,
        SOCCER,
        SOFTBALL,
        ULTIMATE_DISC,
        VOLLEYBALL,
        RACKET_SPORTS,
        BADMINTON,
        PADDELBALL,
        PICKLEBALL,
        PLATFORM_TENNIS,
        RACQUETBALL,
        SQUASH,
        TABLE_TENNIS,
        TENNIS,
        TENNIS_V2,
        OTHER,
        BOXING,
        BREATHWORK,
        DANCE,
        DISC_GOLF,
        FLOOR_CLIMBING,
        GOLF,
        INLINE_SKATING,
        JUMP_ROPE,
        MIXED_MARTIAL_ARTS,
        MOUNTAINEERING,
        ROCK_CLIMBING,
        STOP_WATCH,
        PARA_SPORTS,
        WHEELCHAIR_PUSH_RUN,
        WHEELCHAIR_PUSH_WALK
    }
}

// Models/GarminWebhookPayload.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourApp.Models
{
    public class GarminWebhookPayload
    {
        public int Id { get; set; }
        
        [Required]
        public GarminWebhookType WebhookType { get; set; }
        
        [Required]
        [Column(TypeName = "jsonb")]
        public string RawPayload { get; set; } = string.Empty;
        
        [Required]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsProcessed { get; set; } = false;
        
        public DateTime? ProcessedAt { get; set; }
        
        public string? ProcessingError { get; set; }
        
        public int? RetryCount { get; set; } = 0;
        
        public DateTime? NextRetryAt { get; set; }
    }

    public enum GarminWebhookType
    {
        Activities,
        ActivityDetails,
        ActivityFiles,
        ManuallyUpdatedActivities,
        MoveIQActivities
    }
}
```

### 2. DbContext Updates

```csharp
// Data/ApplicationDbContext.cs - Add to existing context
public DbSet<GarminActivity> GarminActivities { get; set; }
public DbSet<GarminWebhookPayload> GarminWebhookPayloads { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Existing configurations...
    
    modelBuilder.Entity<GarminActivity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.SummaryId).IsUnique();
        entity.HasIndex(e => e.ActivityType);
        entity.HasIndex(e => e.StartTime);
        entity.HasIndex(e => e.IsProcessed);
        entity.HasIndex(e => new { e.UserId, e.ActivityType });
        
        entity.Property(e => e.ActivityType)
            .HasConversion<string>();
    });
    
    modelBuilder.Entity<GarminWebhookPayload>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.WebhookType);
        entity.HasIndex(e => e.IsProcessed);
        entity.HasIndex(e => e.ReceivedAt);
        entity.HasIndex(e => e.NextRetryAt);
        
        entity.Property(e => e.WebhookType)
            .HasConversion<string>();
    });
    
    base.OnModelCreating(modelBuilder);
}
```

## Services Implementation

### 1. Webhook Processing Service

```csharp
// Services/GarminWebhookService.cs
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Services
{
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

        public GarminWebhookService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<GarminWebhookService> logger,
            IGarminActivityProcessingService activityProcessingService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _activityProcessingService = activityProcessingService;
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

                if (root.TryGetProperty("activities", out var activitiesElement))
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
                string userId = ExtractUserId(activityElement);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Could not extract userId from activity {SummaryId}", summaryId);
                    return;
                }

                // Parse activity data
                var activity = ParseActivity(activityElement, userId);
                
                _context.GarminActivities.Add(activity);
                await _context.SaveChangesAsync();

                // Process for challenges asynchronously
                _ = Task.Run(async () => await _activityProcessingService.ProcessActivityForChallengesAsync(activity.Id));

                _logger.LogInformation("Successfully processed activity {SummaryId} for user {UserId}", summaryId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing single activity from payload {PayloadId}", payloadId);
                throw;
            }
        }

        private string ExtractUserId(JsonElement activityElement)
        {
            // Try userAccessToken first (from push notifications)
            if (activityElement.TryGetProperty("userAccessToken", out var uatElement))
            {
                return uatElement.GetString() ?? "";
            }
            
            // Try userId (from ping notifications)
            if (activityElement.TryGetProperty("userId", out var userIdElement))
            {
                return userIdElement.GetString() ?? "";
            }
            
            return "";
        }

        private GarminActivity ParseActivity(JsonElement activityElement, string userId)
        {
            var activity = new GarminActivity
            {
                UserId = userId,
                SummaryId = activityElement.GetProperty("summaryId").GetString() ?? "",
                ResponseData = activityElement.GetRawText()
            };

            // Parse optional fields
            if (activityElement.TryGetProperty("activityId", out var activityIdElement))
                activity.ActivityId = activityIdElement.GetString();

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
}
```

### 2. Activity Processing Service

```csharp
// Services/GarminActivityProcessingService.cs
using Microsoft.EntityFrameworkCore;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Services
{
    public interface IGarminActivityProcessingService
    {
        Task ProcessActivityForChallengesAsync(int activityId);
        Task<List<GarminActivity>> GetCyclingActivitiesAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<List<GarminActivity>> GetUserActivitiesAsync(string userId, int page = 1, int pageSize = 20);
    }

    public class GarminActivityProcessingService : IGarminActivityProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GarminActivityProcessingService> _logger;
        // Add your challenge service here
        // private readonly IChallengeService _challengeService;

        public GarminActivityProcessingService(
            ApplicationDbContext context,
            ILogger<GarminActivityProcessingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessActivityForChallengesAsync(int activityId)
        {
            try
            {
                var activity = await _context.GarminActivities
                    .FirstOrDefaultAsync(a => a.Id == activityId);

                if (activity == null)
                {
                    _logger.LogWarning("Activity {ActivityId} not found for challenge processing", activityId);
                    return;
                }

                // Get user's active challenges
                // var userChallenges = await _challengeService.GetActiveChallengesForUserAsync(activity.UserId);

                // Process each challenge
                // foreach (var challenge in userChallenges)
                // {
                //     await _challengeService.ProcessActivityForChallengeAsync(challenge.Id, activity);
                // }

                // Mark as processed
                activity.IsProcessed = true;
                activity.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed activity {ActivityId} for challenges", activityId);
            }
            catch (Exception ex)
            {
                var activity = await _context.GarminActivities.FindAsync(activityId);
                if (activity != null)
                {
                    activity.ProcessingError = ex.Message;
                    await _context.SaveChangesAsync();
                }

                _logger.LogError(ex, "Error processing activity {ActivityId} for challenges", activityId);
            }
        }

        public async Task<List<GarminActivity>> GetCyclingActivitiesAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            var cyclingActivityTypes = new[]
            {
                GarminActivityType.CYCLING,
                GarminActivityType.BMX,
                GarminActivityType.CYCLOCROSS,
                GarminActivityType.DOWNHILL_BIKING,
                GarminActivityType.E_BIKE_FITNESS,
                GarminActivityType.E_BIKE_MOUNTAIN,
                GarminActivityType.E_ENDURO_MTB,
                GarminActivityType.ENDURO_MTB,
                GarminActivityType.GRAVEL_CYCLING,
                GarminActivityType.INDOOR_CYCLING,
                GarminActivityType.MOUNTAIN_BIKING,
                GarminActivityType.RECUMBENT_CYCLING,
                GarminActivityType.ROAD_BIKING,
                GarminActivityType.TRACK_CYCLING,
                GarminActivityType.VIRTUAL_RIDE,
                GarminActivityType.HANDCYCLING,
                GarminActivityType.INDOOR_HANDCYCLING
            };

            return await _context.GarminActivities
                .Where(a => a.UserId == userId &&
                           cyclingActivityTypes.Contains(a.ActivityType) &&
                           a.StartTime >= fromDate &&
                           a.StartTime <= toDate)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<List<GarminActivity>> GetUserActivitiesAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _context.GarminActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
```

## Controller Implementation

### Webhook Controller

```csharp
// Controllers/GarminWebhookController.cs
using Microsoft.AspNetCore.Mvc;
using YourApp.Services;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GarminWebhookController : ControllerBase
    {
        private readonly IGarminWebhookService _webhookService;
        private readonly ILogger<GarminWebhookController> _logger;

        public GarminWebhookController(
            IGarminWebhookService webhookService,
            ILogger<GarminWebhookController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost("ping/{webhookType}")]
        public async Task<IActionResult> HandlePingWebhook(string webhookType)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                string payload = await reader.ReadToEndAsync();

                _logger.LogInformation("Received ping webhook for type {WebhookType}", webhookType);

                // Immediately return 200 OK as required by Garmin
                var processingTask = _webhookService.ProcessPingNotificationAsync(webhookType, payload);
                
                // Don't await - process asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await processingTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ping webhook asynchronously");
                    }
                });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ping webhook for type {WebhookType}", webhookType);
                return Ok(); // Still return 200 to avoid retries
            }
        }

        [HttpPost("push/{webhookType}")]
        public async Task<IActionResult> HandlePushWebhook(string webhookType)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                string payload = await reader.ReadToEndAsync();

                _logger.LogInformation("Received push webhook for type {WebhookType}", webhookType);

                // Immediately return 200 OK as required by Garmin
                var processingTask = _webhookService.ProcessPushNotificationAsync(webhookType, payload);
                
                // Don't await - process asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await processingTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing push webhook asynchronously");
                    }
                });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling push webhook for type {WebhookType}", webhookType);
                return Ok(); // Still return 200 to avoid retries
            }
        }

        // Endpoint for manual processing of failed payloads
        [HttpPost("process-failed")]
        public async Task<IActionResult> ProcessFailedPayloads()
        {
            try
            {
                await _webhookService.ProcessStoredPayloadsAsync();
                return Ok(new { message = "Failed payloads processing initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing failed payloads");
                return StatusCode(500, new { error = "Failed to process failed payloads" });
            }
        }
    }
}
```

## Background Service for Retry Processing

```csharp
// Services/GarminWebhookBackgroundService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YourApp.Services;

namespace YourApp.Services
{
    public class GarminWebhookBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GarminWebhookBackgroundService> _logger;

        public GarminWebhookBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<GarminWebhookBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var webhookService = scope.ServiceProvider.GetRequiredService<IGarminWebhookService>();
                    
                    await webhookService.ProcessStoredPayloadsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Garmin webhook background processing");
                }

                // Wait 5 minutes before next processing
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

## Program.cs Configuration

```csharp
// Program.cs additions
using YourApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Existing services...

// Add Garmin webhook services
builder.Services.AddScoped<IGarminWebhookService, GarminWebhookService>();
builder.Services.AddScoped<IGarminActivityProcessingService, GarminActivityProcessingService>();

// Add background service for retry processing
builder.Services.AddHostedService<GarminWebhookBackgroundService>();

var app = builder.Build();

// Configure pipeline...
```

## Database Migration

```sql
-- PostgreSQL migration for webhook support
CREATE TABLE garmin_activities (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    summary_id VARCHAR(255) NOT NULL UNIQUE,
    activity_id VARCHAR(255),
    activity_type VARCHAR(100) NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    start_time_offset_in_seconds INTEGER DEFAULT 0,
    duration_in_seconds INTEGER NOT NULL,
    distance_in_meters DOUBLE PRECISION,
    total_elevation_gain_in_meters DOUBLE PRECISION,
    total_elevation_loss_in_meters DOUBLE PRECISION,
    active_kilocalories INTEGER,
    device_name VARCHAR(255),
    is_manual BOOLEAN DEFAULT FALSE,
    is_web_upload BOOLEAN DEFAULT FALSE,
    response_data JSONB NOT NULL,
    received_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE,
    is_processed BOOLEAN DEFAULT FALSE,
    processing_error TEXT
);

CREATE TABLE garmin_webhook_payloads (
    id SERIAL PRIMARY KEY,
    webhook_type VARCHAR(50) NOT NULL,
    raw_payload JSONB NOT NULL,
    received_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMP WITH TIME ZONE,
    processing_error TEXT,
    retry_count INTEGER DEFAULT 0,
    next_retry_at TIMESTAMP WITH TIME ZONE
);

-- Indexes for garmin_activities
CREATE INDEX idx_garmin_activities_user_id ON garmin_activities(user_id);
CREATE INDEX idx_garmin_activities_summary_id ON garmin_activities(summary_id);
CREATE INDEX idx_garmin_activities_activity_type ON garmin_activities(activity_type);
CREATE INDEX idx_garmin_activities_start_time ON garmin_activities(start_time);
CREATE INDEX idx_garmin_activities_is_processed ON garmin_activities(is_processed);
CREATE INDEX idx_garmin_activities_user_activity_type ON garmin_activities(user_id, activity_type);

-- Indexes for garmin_webhook_payloads
CREATE INDEX idx_garmin_webhook_payloads_webhook_type ON garmin_webhook_payloads(webhook_type);
CREATE INDEX idx_garmin_webhook_payloads_is_processed ON garmin_webhook_payloads(is_processed);
CREATE INDEX idx_garmin_webhook_payloads_received_at ON garmin_webhook_payloads(received_at);
CREATE INDEX idx_garmin_webhook_payloads_next_retry_at ON garmin_webhook_payloads(next_retry_at);
```

## Garmin Endpoint Configuration

### Webhook URLs to Configure in Garmin Developer Portal

Configure these URLs in the Garmin Endpoint Configuration Tool at https://apis.garmin.com/tools/endpoints:

**For Ping Service (recommended):**
- **Activities**: `https://yourdomain.com/api/garminwebhook/ping/activities`
- **Activity Details**: `https://yourdomain.com/api/garminwebhook/ping/activitydetails`
- **Activity Files**: `https://yourdomain.com/api/garminwebhook/ping/activityfiles`
- **Manually Updated Activities**: `https://yourdomain.com/api/garminwebhook/ping/manuallyupdatedactivities`
- **Move IQ Activities**: `https://yourdomain.com/api/garminwebhook/ping/moveiqactivities`

**For Push Service (alternative):**
- **Activities**: `https://yourdomain.com/api/garminwebhook/push/activities`
- **Activity Details**: `https://yourdomain.com/api/garminwebhook/push/activitydetails`
- **Activity Files**: `https://yourdomain.com/api/garminwebhook/push/activityfiles`
- **Manually Updated Activities**: `https://yourdomain.com/api/garminwebhook/push/manuallyupdatedactivities`
- **Move IQ Activities**: `https://yourdomain.com/api/garminwebhook/push/moveiqactivities`

## Frontend Integration

### Vue.js Component for Activity Display

```javascript
// ActivityList.vue
<template>
  <div class="activity-list">
    <h2>Your Garmin Activities</h2>
    
    <div v-if="loading" class="loading">
      Loading activities...
    </div>
    
    <div v-else-if="activities.length === 0" class="no-activities">
      No activities found. Sync your Garmin device to see activities here.
    </div>
    
    <div v-else class="activities">
      <div 
        v-for="activity in activities" 
        :key="activity.id"
        class="activity-card"
        :class="{ 'cycling': isCyclingActivity(activity.activityType) }"
      >
        <div class="activity-header">
          <h3>{{ formatActivityType(activity.activityType) }}</h3>
          <span class="activity-date">{{ formatDate(activity.startTime) }}</span>
        </div>
        
        <div class="activity-stats">
          <div v-if="activity.distanceInMeters" class="stat">
            <span class="label">Distance:</span>
            <span class="value">{{ formatDistance(activity.distanceInMeters) }}</span>
          </div>
          
          <div class="stat">
            <span class="label">Duration:</span>
            <span class="value">{{ formatDuration(activity.durationInSeconds) }}</span>
          </div>
          
          <div v-if="activity.totalElevationGainInMeters" class="stat">
            <span class="label">Elevation:</span>
            <span class="value">{{ Math.round(activity.totalElevationGainInMeters) }}m</span>
          </div>
          
          <div v-if="activity.activeKilocalories" class="stat">
            <span class="label">Calories:</span>
            <span class="value">{{ activity.activeKilocalories }}</span>
          </div>
        </div>
        
        <div v-if="activity.deviceName" class="device-info">
          <small>Recorded on {{ activity.deviceName }}</small>
        </div>
        
        <div v-if="activity.isManual || activity.isWebUpload" class="manual-indicator">
          <small>{{ activity.isManual ? 'Manual Entry' : 'Web Upload' }}</small>
        </div>
      </div>
    </div>
    
    <div v-if="hasMoreActivities" class="load-more">
      <button @click="loadMoreActivities" :disabled="loadingMore">
        {{ loadingMore ? 'Loading...' : 'Load More' }}
      </button>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ActivityList',
  data() {
    return {
      activities: [],
      loading: false,
      loadingMore: false,
      currentPage: 1,
      hasMoreActivities: true
    };
  },
  
  async mounted() {
    await this.loadActivities();
  },
  
  methods: {
    async loadActivities() {
      this.loading = true;
      try {
        const response = await this.$http.get('/api/garminactivities', {
          params: { page: 1, pageSize: 20 }
        });
        
        this.activities = response.data.activities || [];
        this.hasMoreActivities = response.data.hasMore || false;
        this.currentPage = 1;
      } catch (error) {
        console.error('Failed to load activities:', error);
        this.$swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'Failed to load activities'
        });
      } finally {
        this.loading = false;
      }
    },
    
    async loadMoreActivities() {
      this.loadingMore = true;
      try {
        const response = await this.$http.get('/api/garminactivities', {
          params: { page: this.currentPage + 1, pageSize: 20 }
        });
        
        this.activities.push(...(response.data.activities || []));
        this.hasMoreActivities = response.data.hasMore || false;
        this.currentPage++;
      } catch (error) {
        console.error('Failed to load more activities:', error);
      } finally {
        this.loadingMore = false;
      }
    },
    
    isCyclingActivity(activityType) {
      const cyclingTypes = [
        'CYCLING', 'BMX', 'CYCLOCROSS', 'DOWNHILL_BIKING', 
        'E_BIKE_FITNESS', 'E_BIKE_MOUNTAIN', 'E_ENDURO_MTB', 
        'ENDURO_MTB', 'GRAVEL_CYCLING', 'INDOOR_CYCLING', 
        'MOUNTAIN_BIKING', 'RECUMBENT_CYCLING', 'ROAD_BIKING', 
        'TRACK_CYCLING', 'VIRTUAL_RIDE', 'HANDCYCLING', 'INDOOR_HANDCYCLING'
      ];
      return cyclingTypes.includes(activityType);
    },
    
    formatActivityType(activityType) {
      return activityType
        .replace(/_/g, ' ')
        .toLowerCase()
        .replace(/\b\w/g, l => l.toUpperCase());
    },
    
    formatDate(dateString) {
      return new Date(dateString).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    },
    
    formatDistance(meters) {
      const km = meters / 1000;
      return km >= 1 ? `${km.toFixed(2)} km` : `${Math.round(meters)} m`;
    },
    
    formatDuration(seconds) {
      const hours = Math.floor(seconds / 3600);
      const minutes = Math.floor((seconds % 3600) / 60);
      const remainingSeconds = seconds % 60;
      
      if (hours > 0) {
        return `${hours}h ${minutes}m`;
      } else if (minutes > 0) {
        return `${minutes}m ${remainingSeconds}s`;
      } else {
        return `${remainingSeconds}s`;
      }
    }
  }
};
</script>

<style scoped>
.activity-list {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.activity-card {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 16px;
  background: white;
}

.activity-card.cycling {
  border-left: 4px solid #2196F3;
}

.activity-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.activity-header h3 {
  margin: 0;
  color: #333;
}

.activity-date {
  color: #666;
  font-size: 14px;
}

.activity-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 12px;
  margin-bottom: 12px;
}

.stat {
  display: flex;
  flex-direction: column;
}

.stat .label {
  font-size: 12px;
  color: #666;
  margin-bottom: 2px;
}

.stat .value {
  font-weight: bold;
  color: #333;
}

.device-info, .manual-indicator {
  margin-top: 8px;
}

.device-info small, .manual-indicator small {
  color: #888;
}

.manual-indicator small {
  font-style: italic;
}

.load-more {
  text-align: center;
  margin-top: 20px;
}

.load-more button {
  padding: 10px 20px;
  background: #2196F3;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.load-more button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.loading, .no-activities {
  text-align: center;
  padding: 40px;
  color: #666;
}
</style>
```

### API Controller for Frontend

```csharp
// Controllers/GarminActivitiesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using YourApp.Services;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GarminActivitiesController : ControllerBase
    {
        private readonly IGarminActivityProcessingService _activityService;
        private readonly ILogger<GarminActivitiesController> _logger;

        public GarminActivitiesController(
            IGarminActivityProcessingService activityService,
            ILogger<GarminActivitiesController> logger)
        {
            _activityService = activityService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserActivities(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                if (pageSize > 50) pageSize = 50; // Limit page size

                var activities = await _activityService.GetUserActivitiesAsync(userId, page, pageSize);
                
                // Check if there are more activities
                var nextPageActivities = await _activityService.GetUserActivitiesAsync(userId, page + 1, 1);
                bool hasMore = nextPageActivities.Any();

                return Ok(new
                {
                    activities = activities.Select(a => new
                    {
                        a.Id,
                        a.SummaryId,
                        a.ActivityId,
                        a.ActivityType,
                        a.StartTime,
                        a.DurationInSeconds,
                        a.DistanceInMeters,
                        a.TotalElevationGainInMeters,
                        a.TotalElevationLossInMeters,
                        a.ActiveKilocalories,
                        a.DeviceName,
                        a.IsManual,
                        a.IsWebUpload,
                        a.ReceivedAt
                    }),
                    hasMore,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activities");
                return StatusCode(500, new { error = "Failed to get activities" });
            }
        }

        [HttpGet("cycling")]
        public async Task<IActionResult> GetCyclingActivities(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                fromDate ??= DateTime.UtcNow.AddMonths(-3);
                toDate ??= DateTime.UtcNow;

                var activities = await _activityService.GetCyclingActivitiesAsync(userId, fromDate.Value, toDate.Value);

                return Ok(new
                {
                    activities = activities.Select(a => new
                    {
                        a.Id,
                        a.SummaryId,
                        a.ActivityId,
                        a.ActivityType,
                        a.StartTime,
                        a.DurationInSeconds,
                        a.DistanceInMeters,
                        a.TotalElevationGainInMeters,
                        a.TotalElevationLossInMeters,
                        a.ActiveKilocalories,
                        a.DeviceName,
                        a.IsManual,
                        a.IsWebUpload
                    }),
                    fromDate,
                    toDate,
                    totalActivities = activities.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cycling activities");
                return StatusCode(500, new { error = "Failed to get cycling activities" });
            }
        }

        [HttpGet("{activityId}/details")]
        public async Task<IActionResult> GetActivityDetails(int activityId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var activity = await _activityService.GetActivityDetailsAsync(activityId, userId);
                
                if (activity == null)
                {
                    return NotFound("Activity not found");
                }

                return Ok(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity details for {ActivityId}", activityId);
                return StatusCode(500, new { error = "Failed to get activity details" });
            }
        }
    }
}
```

## Testing & Validation

### 1. Local Testing with ngrok

```bash
# Install ngrok for local testing
npm install -g ngrok

# Expose your local development server
ngrok http 5000

# Use the ngrok URL in Garmin endpoint configuration
# Example: https://abc123.ngrok.io/api/garminwebhook/ping/activities
```

### 2. Test Webhook Processing

```csharp
// Create a test endpoint to simulate Garmin webhooks
[HttpPost("test/webhook")]
public async Task<IActionResult> TestWebhook()
{
    var testPayload = @"{
        ""activities"": [{
            ""userId"": ""test-user-123"",
            ""summaryId"": ""test-summary-456"",
            ""activityType"": ""CYCLING"",
            ""startTimeInSeconds"": 1640995200,
            ""durationInSeconds"": 3600,
            ""distanceInMeters"": 25000,
            ""activeKilocalories"": 800,
            ""deviceName"": ""Test Device""
        }]
    }";

    var result = await _webhookService.ProcessPushNotificationAsync("activities", testPayload);
    
    return Ok(new { success = result });
}
```

## Security Considerations

1. **Webhook Authentication**: While Garmin doesn't provide webhook signatures, validate that requests come from expected sources
2. **Rate Limiting**: Implement rate limiting on webhook endpoints
3. **Duplicate Prevention**: The system handles duplicates by checking `summaryId`
4. **Data Validation**: Validate all incoming data before storage
5. **Error Handling**: Proper error handling without exposing sensitive information

## Monitoring & Alerting

1. **Webhook Health**: Monitor webhook endpoint availability
2. **Processing Delays**: Alert on processing delays or failures
3. **Data Quality**: Monitor for missing or malformed data
4. **Storage Growth**: Monitor database growth and cleanup old data

## Production Deployment Steps

1. **Deploy the code** to your production environment
2. **Run database migrations** to create the new tables
3. **Configure Garmin endpoints** using your production URLs
4. **Test the webhook flow** with real Garmin data
5. **Monitor webhook processing** and adjust retry logic as needed
6. **Set up alerting** for webhook failures

## Next Steps

1. Implement challenge processing logic in `IGarminActivityProcessingService`
2. Add data cleanup job for old webhook payloads
3. Implement webhook authentication if needed
4. Add more detailed activity analytics
5. Consider implementing activity file processing for detailed GPS data