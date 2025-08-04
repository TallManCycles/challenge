using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GarminActivitiesController : ControllerBase
{
    private readonly IGarminActivityProcessingService _activityService;
    private readonly IFileLoggingService _logger;

    public GarminActivitiesController(
        IGarminActivityProcessingService activityService,
        IFileLoggingService logger)
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
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
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
                    ActivityType = a.ActivityType.ToString(),
                    StartTime = DateTime.SpecifyKind(a.StartTime, DateTimeKind.Utc),
                    a.DurationInSeconds,
                    a.DistanceInMeters,
                    a.TotalElevationGainInMeters,
                    a.TotalElevationLossInMeters,
                    a.ActiveKilocalories,
                    a.DeviceName,
                    a.IsManual,
                    a.IsWebUpload,
                    ReceivedAt = DateTime.SpecifyKind(a.ReceivedAt, DateTimeKind.Utc)
                }),
                hasMore,
                page,
                pageSize
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error getting user activities", ex, "GarminActivitiesController");
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
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
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
                    ActivityType = a.ActivityType.ToString(),
                    StartTime = DateTime.SpecifyKind(a.StartTime, DateTimeKind.Utc),
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
            await _logger.LogErrorAsync("Error getting cycling activities",ex, "GarminActivitiesController");
            return StatusCode(500, new { error = "Failed to get cycling activities" });
        }
    }

    [HttpGet("{activityId}/details")]
    public async Task<IActionResult> GetActivityDetails(int activityId)
    {
        try
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var activity = await _activityService.GetActivityDetailsAsync(activityId, userId);
            
            if (activity == null)
            {
                return NotFound("Activity not found");
            }

            return Ok(new
            {
                activity.Id,
                activity.SummaryId,
                activity.ActivityId,
                ActivityType = activity.ActivityType.ToString(),
                StartTime = DateTime.SpecifyKind(activity.StartTime, DateTimeKind.Utc),
                activity.StartTimeOffsetInSeconds,
                activity.DurationInSeconds,
                activity.DistanceInMeters,
                activity.TotalElevationGainInMeters,
                activity.TotalElevationLossInMeters,
                activity.ActiveKilocalories,
                activity.DeviceName,
                activity.IsManual,
                activity.IsWebUpload,
                ReceivedAt = DateTime.SpecifyKind(activity.ReceivedAt, DateTimeKind.Utc),
                ProcessedAt = activity.ProcessedAt.HasValue ? DateTime.SpecifyKind(activity.ProcessedAt.Value, DateTimeKind.Utc) : (DateTime?)null,
                activity.IsProcessed
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error getting activity details for {activityId}",ex,"GarminActivitiesController" );
            return StatusCode(500, new { error = "Failed to get activity details" });
        }
    }
}