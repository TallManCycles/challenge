using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT authentication
public class GarminDailyFetchController : ControllerBase
{
    private readonly IGarminDailyActivityFetchService _dailyFetchService;
    private readonly IFileLoggingService _logger;

    public GarminDailyFetchController(
        IGarminDailyActivityFetchService dailyFetchService,
        IFileLoggingService logger)
    {
        _dailyFetchService = dailyFetchService;
        _logger = logger;
    }

    [HttpPost("trigger-all")]
    public async Task<IActionResult> TriggerDailyFetchForAllUsers()
    {
        try
        {
            await _logger.LogInfoAsync("Manual trigger of daily activity fetch for all users");
            
            // Run the daily fetch in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _dailyFetchService.FetchActivitiesForAllUsersAsync();
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("Error during manual daily fetch execution", ex, "GarminDailyFetchController");
                }
            });

            return Ok(new { message = "Daily activity fetch initiated for all users" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error triggering daily activity fetch for all users", ex, "GarminDailyFetchController");
            return StatusCode(500, new { error = "Failed to trigger daily fetch" });
        }
    }

    [HttpPost("trigger-user")]
    public async Task<IActionResult> TriggerDailyFetchForCurrentUser()
    {
        try
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("User ID not found in token");
            }

            await _logger.LogInfoAsync($"Manual trigger of daily activity fetch for user {userId}");
            
            // Run the daily fetch in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _dailyFetchService.FetchActivitiesForUserAsync(userId);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("Error during manual daily fetch execution for user {UserId}", ex, "GarminDailyFetchController");
                }
            });

            return Ok(new { message = "Daily activity fetch initiated for current user" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error triggering daily activity fetch for current user", ex, "GarminDailyFetchController");
            return StatusCode(500, new { error = "Failed to trigger daily fetch" });
        }
    }
}