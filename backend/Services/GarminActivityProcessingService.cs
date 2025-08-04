using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Services;

public interface IGarminActivityProcessingService
{
    Task ProcessActivityForChallengesAsync(int activityId);
    Task<List<GarminActivity>> GetCyclingActivitiesAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<List<GarminActivity>> GetUserActivitiesAsync(int userId, int page = 1, int pageSize = 20);
    Task<GarminActivity?> GetActivityDetailsAsync(int activityId, int userId);
}

public class GarminActivityProcessingService : IGarminActivityProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GarminActivityProcessingService> _logger;

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

            // Get user's active challenges where they are participants
            var userChallenges = await _context.ChallengeParticipants
                .Include(cp => cp.Challenge)
                .Where(cp => cp.UserId == activity.UserId && 
                           cp.Challenge.IsActive &&
                           cp.Challenge.StartDate <= activity.StartTime &&
                           cp.Challenge.EndDate >= activity.StartTime)
                .ToListAsync();

            // Process each challenge
            foreach (var participation in userChallenges)
            {
                await ProcessActivityForSpecificChallengeAsync(participation, activity);
            }

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

    private async Task ProcessActivityForSpecificChallengeAsync(ChallengeParticipant participation, GarminActivity activity)
    {
        try
        {
            var challenge = participation.Challenge;
            double activityValue = 0;

            // Calculate the value based on challenge type
            switch (challenge.ChallengeType)
            {
                case ChallengeType.Distance:
                    if (activity.DistanceInMeters.HasValue)
                    {
                        activityValue = activity.DistanceInMeters.Value / 1000; // Convert to kilometers
                    }
                    break;
                    
                case ChallengeType.Elevation:
                    if (activity.TotalElevationGainInMeters.HasValue)
                    {
                        activityValue = activity.TotalElevationGainInMeters.Value; // Keep in meters
                    }
                    break;
                    
                case ChallengeType.Time:
                    activityValue = activity.DurationInSeconds / 3600.0; // Convert to hours
                    break;
            }

            // Only count cycling activities if this is relevant to the challenge
            if (activityValue > 0 && IsCyclingActivity(activity.ActivityType))
            {
                participation.CurrentTotal += (decimal)activityValue;
                participation.LastActivityDate = activity.StartTime;
                
                _logger.LogInformation("Updated challenge {ChallengeId} for user {UserId}: added {Value} {Type}", 
                    challenge.Id, activity.UserId, activityValue, challenge.ChallengeType);
                
                // Insert the activity into the Activities table if it doesn't already exist
                var existingActivity = await _context.Activities
                    .FirstOrDefaultAsync(a => a.GarminActivityId == activity.ActivityId);
                
                if (existingActivity == null)
                {
                    var newActivity = new Activity
                    {
                        UserId = activity.UserId,
                        GarminActivityId = activity.ActivityId ?? activity.SummaryId,
                        ActivityName = activity.ActivityName ?? $"{activity.ActivityType} Activity",
                        Distance = (decimal)(activity.DistanceInMeters ?? 0) / 1000, // Convert to km
                        ElevationGain = (decimal)(activity.TotalElevationGainInMeters ?? 0),
                        MovingTime = activity.DurationInSeconds,
                        ActivityDate = activity.StartTime,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    _context.Activities.Add(newActivity);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Added activity {ActivityId} to Activities table for user {UserId}", 
                        newActivity.GarminActivityId, activity.UserId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity {ActivityId} for challenge {ChallengeId}", 
                activity.Id, participation.ChallengeId);
        }
    }

    private bool IsCyclingActivity(GarminActivityType activityType)
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

        return cyclingActivityTypes.Contains(activityType);
    }

    public async Task<List<GarminActivity>> GetCyclingActivitiesAsync(int userId, DateTime fromDate, DateTime toDate)
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

    public async Task<List<GarminActivity>> GetUserActivitiesAsync(int userId, int page = 1, int pageSize = 20)
    {
        return await _context.GarminActivities
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<GarminActivity?> GetActivityDetailsAsync(int activityId, int userId)
    {
        return await _context.GarminActivities
            .FirstOrDefaultAsync(a => a.Id == activityId && a.UserId == userId);
    }
}