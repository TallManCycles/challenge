namespace backend.Services;

public interface IChallengeNotificationService
{
    Task SendActivityNotificationAsync(int activityId, int challengeId);
    Task SendActivityNotificationsForAllChallengesAsync(int activityId);
    Task SendChallengeCompletionNotificationsAsync(int challengeId);
    Task CheckAndNotifyCompletedChallengesAsync();
}