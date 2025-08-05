using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using backend.Models;

namespace backend.Services;

public class AwsSesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSesEmailService> _logger;
    private readonly IQuoteService _quoteService;
    private readonly string _fromEmail;

    public AwsSesEmailService(
        IAmazonSimpleEmailService sesClient,
        IConfiguration configuration,
        ILogger<AwsSesEmailService> logger,
        IQuoteService quoteService)
    {
        _sesClient = sesClient;
        _configuration = configuration;
        _logger = logger;
        _quoteService = quoteService;
        _fromEmail = _configuration["Email:FromAddress"] ?? "noreply@challengehub.com";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var sendRequest = new SendEmailRequest
            {
                Source = _fromEmail,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body()
                }
            };

            if (!string.IsNullOrEmpty(htmlBody))
            {
                sendRequest.Message.Body.Html = new Content(htmlBody);
            }

            if (!string.IsNullOrEmpty(textBody))
            {
                sendRequest.Message.Body.Text = new Content(textBody);
            }
            else if (!string.IsNullOrEmpty(htmlBody))
            {
                // Generate simple text version from HTML if no text body provided
                sendRequest.Message.Body.Text = new Content(System.Text.RegularExpressions.Regex.Replace(htmlBody, "<.*?>", string.Empty));
            }

            var response = await _sesClient.SendEmailAsync(sendRequest);
            _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", toEmail, response.MessageId);
        }
        catch (Amazon.SimpleEmail.AmazonSimpleEmailServiceException sesEx)
        {
            if (sesEx.ErrorCode == "MessageRejected" && sesEx.Message.Contains("not authorized"))
            {
                _logger.LogError("AWS SES Permission Error: The IAM user does not have permission to send emails from {FromEmail}. Please verify the email address in AWS SES and ensure the IAM user has ses:SendEmail permissions.", _fromEmail);
            }
            else if (sesEx.ErrorCode == "MessageRejected" && sesEx.Message.Contains("Email address not verified"))
            {
                _logger.LogError("AWS SES Verification Error: Email address {FromEmail} is not verified in AWS SES. Please verify this email address in the AWS SES console.", _fromEmail);
            }
            else
            {
                _logger.LogError(sesEx, "AWS SES Error sending email to {Email}: {ErrorCode} - {Message}", toEmail, sesEx.ErrorCode, sesEx.Message);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendChallengeActivityNotificationAsync(string toEmail, string userName, string activityName, string challengeTitle, decimal activityValue, string challengeType)
    {
        var subject = $"New Activity in Challenge: {challengeTitle}";
        
        // Get a random cyclist quote
        Quote? quote = null;
        try
        {
            quote = await _quoteService.GetRandomQuoteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve quote for email notification");
        }
        
        var quoteSection = quote != null 
            ? $@"
        <div style='border-top: 1px solid #e5e7eb; margin-top: 30px; padding-top: 20px; text-align: center; color: #6b7280; font-style: italic;'>
            <p style='margin: 0; font-size: 16px; line-height: 1.5;'>""{quote.Text}""</p>
            <p style='margin: 10px 0 0 0; font-size: 14px; font-weight: 600;'>— {quote.Author}</p>
        </div>"
            : "";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Challenge Activity Notification</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #3B82F6; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .activity-details {{ background-color: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #3B82F6; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
        .button {{ display: inline-block; background-color: #3B82F6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🚴‍♂️ New Activity Alert!</h1>
    </div>
    <div class='content'>
        <p>Hello!</p>
        <p><strong>{userName}</strong> just completed a new activity in the challenge <strong>{challengeTitle}</strong>!</p>
        
        <div class='activity-details'>
            <h3>📊 Activity Details</h3>
            <ul>
                <li><strong>Activity:</strong> {activityName}</li>
                <li><strong>Challenge Type:</strong> {challengeType}</li>
                <li><strong>Value Added:</strong> {activityValue:F2} {GetUnitForChallengeType(challengeType)}</li>
            </ul>
        </div>

        <p>Keep up the great work and stay motivated! 💪</p>
        
        <div class='footer'>
            <p>You're receiving this email because you have notifications enabled for challenges.</p>
            <p>To manage your notification preferences, visit your settings page.</p>
        </div>
        {quoteSection}
    </div>
</body>
</html>";

        var quoteText = quote != null ? $@"

""{quote.Text}""
— {quote.Author}" : "";

        var textBody = $@"
New Activity Alert!

{userName} just completed a new activity in the challenge {challengeTitle}!

Activity Details:
- Activity: {activityName}
- Challenge Type: {challengeType}
- Value Added: {activityValue:F2} {GetUnitForChallengeType(challengeType)}

Keep up the great work and stay motivated!

You're receiving this email because you have notifications enabled for challenges.
To manage your notification preferences, visit your settings page.{quoteText}
";

        await SendEmailAsync(toEmail, subject, htmlBody, textBody);
    }

    private static string GetUnitForChallengeType(string challengeType)
    {
        return challengeType.ToLower() switch
        {
            "distance" => "km",
            "elevation" => "m",
            "time" => "hours",
            _ => "units"
        };
    }
}