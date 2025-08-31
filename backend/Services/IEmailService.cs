namespace backend.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
    Task SendChallengeActivityNotificationAsync(string toEmail, string userName, string activityName, string challengeTitle, decimal activityValue, string challengeType);
    Task SendChallengeCompletionNotificationAsync(string toEmail, string challengeTitle, string participantName, int position, int totalParticipants, List<(string Username, string FullName, decimal Total, int Position)> leaderboard);
}