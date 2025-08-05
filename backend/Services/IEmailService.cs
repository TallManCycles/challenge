namespace backend.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
    Task SendChallengeActivityNotificationAsync(string toEmail, string userName, string activityName, string challengeTitle, decimal activityValue, string challengeType);
}