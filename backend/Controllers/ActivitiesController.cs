using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileLoggingService _logger;

    public ActivitiesController(ApplicationDbContext context, IFileLoggingService logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    [HttpPost("{activityId}/like")]
    public async Task<IActionResult> LikeActivity(int activityId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Check if activity exists
            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null)
            {
                return NotFound(new { error = "Activity not found" });
            }

            // Check if user already liked this activity
            var existingLike = await _context.ActivityLikes
                .FirstOrDefaultAsync(al => al.ActivityId == activityId && al.UserId == currentUserId);

            if (existingLike != null)
            {
                return BadRequest(new { error = "Activity already liked" });
            }

            // Create the like
            var like = new ActivityLike
            {
                ActivityId = activityId,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLikes.Add(like);
            await _context.SaveChangesAsync();

            // Get updated like count
            var likeCount = await _context.ActivityLikes
                .CountAsync(al => al.ActivityId == activityId);

            await _logger.LogInfoAsync($"User {currentUserId} liked activity {activityId}", "ActivitiesController");

            return Ok(new { 
                message = "Activity liked successfully", 
                likeCount = likeCount,
                isLiked = true 
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error liking activity {activityId}", ex, "ActivitiesController");
            return StatusCode(500, new { error = "Failed to like activity" });
        }
    }

    [HttpDelete("{activityId}/like")]
    public async Task<IActionResult> UnlikeActivity(int activityId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Find the like
            var like = await _context.ActivityLikes
                .FirstOrDefaultAsync(al => al.ActivityId == activityId && al.UserId == currentUserId);

            if (like == null)
            {
                return NotFound(new { error = "Like not found" });
            }

            // Remove the like
            _context.ActivityLikes.Remove(like);
            await _context.SaveChangesAsync();

            // Get updated like count
            var likeCount = await _context.ActivityLikes
                .CountAsync(al => al.ActivityId == activityId);

            await _logger.LogInfoAsync($"User {currentUserId} unliked activity {activityId}", "ActivitiesController");

            return Ok(new { 
                message = "Activity unliked successfully", 
                likeCount = likeCount,
                isLiked = false 
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error unliking activity {activityId}", ex, "ActivitiesController");
            return StatusCode(500, new { error = "Failed to unlike activity" });
        }
    }

    [HttpGet("{activityId}/likes")]
    public async Task<IActionResult> GetActivityLikes(int activityId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Check if activity exists
            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null)
            {
                return NotFound(new { error = "Activity not found" });
            }

            // Get like count and current user's like status
            var likeCount = await _context.ActivityLikes
                .CountAsync(al => al.ActivityId == activityId);

            var isLikedByCurrentUser = await _context.ActivityLikes
                .AnyAsync(al => al.ActivityId == activityId && al.UserId == currentUserId);

            return Ok(new { 
                likeCount = likeCount,
                isLiked = isLikedByCurrentUser 
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error getting likes for activity {activityId}", ex, "ActivitiesController");
            return StatusCode(500, new { error = "Failed to get activity likes" });
        }
    }
}