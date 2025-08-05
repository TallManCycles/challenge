using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Services;

public class ChallengeNotificationService : IChallengeNotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<ChallengeNotificationService> _logger;

    public ChallengeNotificationService(
        IServiceProvider serviceProvider,
        IEmailService emailService,
        ILogger<ChallengeNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendActivityNotificationAsync(int activityId, int challengeId)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            // Get the activity and challenge details
            var activity = await context.Activities
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
            {
                _logger.LogWarning("Activity {ActivityId} not found for notification", activityId);
                return;
            }

            var challenge = await context.Challenges
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == challengeId);

            if (challenge == null)
            {
                _logger.LogWarning("Challenge {ChallengeId} not found for notification", challengeId);
                return;
            }

            // Get challenge participants who have email notifications enabled and aren't the activity creator
            var notificationTargets = challenge.Participants
                .Where(p => p.UserId != activity.UserId && p.User.EmailNotificationsEnabled)
                .Select(p => p.User)
                .ToList();

            if (!notificationTargets.Any())
            {
                _logger.LogInformation("No users to notify for activity {ActivityId} in challenge {ChallengeId}", activityId, challengeId);
                return;
            }

            // Calculate activity value based on challenge type
            decimal activityValue = challenge.ChallengeType switch
            {
                ChallengeType.Distance => activity.Distance,
                ChallengeType.Elevation => activity.ElevationGain,
                ChallengeType.Time => activity.MovingTime / 3600m, // Convert seconds to hours
                _ => 0
            };

            // Send notifications to all eligible participants
            var notificationTasks = notificationTargets.Select(async user =>
            {
                try
                {
                    await _emailService.SendChallengeActivityNotificationAsync(
                        user.Email,
                        activity.User.Username,
                        activity.ActivityName,
                        challenge.Title,
                        activityValue,
                        challenge.ChallengeType.ToString()
                    );

                    _logger.LogInformation("Notification sent to {Email} for activity {ActivityId} in challenge {ChallengeId}", 
                        user.Email, activityId, challengeId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send notification to {Email} for activity {ActivityId} in challenge {ChallengeId}", 
                        user.Email, activityId, challengeId);
                }
            });

            await Task.WhenAll(notificationTasks);

            _logger.LogInformation("Completed sending notifications for activity {ActivityId} in challenge {ChallengeId}. Sent to {Count} users.",
                activityId, challengeId, notificationTargets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending activity notification for activity {ActivityId} in challenge {ChallengeId}", 
                activityId, challengeId);
        }
    }

    public async Task SendActivityNotificationsForAllChallengesAsync(int activityId)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            // Get the activity
            var activity = await context.Activities
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
            {
                _logger.LogWarning("Activity {ActivityId} not found for notifications", activityId);
                return;
            }

            // Find all active challenges where this user is a participant and the activity falls within the challenge date range
            var relevantChallenges = await context.ChallengeParticipants
                .Include(cp => cp.Challenge)
                    .ThenInclude(c => c.Participants)
                        .ThenInclude(p => p.User)
                .Where(cp => cp.UserId == activity.UserId && 
                           cp.Challenge.IsActive &&
                           cp.Challenge.StartDate <= activity.ActivityDate &&
                           cp.Challenge.EndDate >= activity.ActivityDate)
                .Select(cp => cp.Challenge)
                .Distinct()
                .ToListAsync();

            if (!relevantChallenges.Any())
            {
                _logger.LogInformation("No relevant challenges found for activity {ActivityId}", activityId);
                return;
            }

            // Send notifications for each relevant challenge
            var notificationTasks = relevantChallenges.Select(challenge =>
                SendActivityNotificationAsync(activityId, challenge.Id)
            );

            await Task.WhenAll(notificationTasks);

            _logger.LogInformation("Completed processing notifications for activity {ActivityId} across {Count} challenges",
                activityId, relevantChallenges.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity notifications for activity {ActivityId}", activityId);
        }
    }
}