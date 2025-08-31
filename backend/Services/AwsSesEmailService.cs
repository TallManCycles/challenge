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

    public async Task SendChallengeCompletionNotificationAsync(string toEmail, string challengeTitle, string participantName, int position, int totalParticipants, List<(string Username, string FullName, decimal Total, int Position)> leaderboard)
    {
        var subject = $"Challenge Complete: {challengeTitle} - Final Results!";
        
        // Determine winner and user's position-specific messaging
        var winner = leaderboard.FirstOrDefault();
        var isWinner = position == 1;
        var positionSuffix = GetPositionSuffix(position);
        
        // Get a random cyclist quote
        Quote? quote = null;
        try
        {
            quote = await _quoteService.GetRandomQuoteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve quote for challenge completion email notification");
        }
        
        var quoteSection = quote != null 
            ? $@"
        <div style='border-top: 1px solid #e5e7eb; margin-top: 30px; padding-top: 20px; text-align: center; color: #6b7280; font-style: italic;'>
            <p style='margin: 0; font-size: 16px; line-height: 1.5;'>""{quote.Text}""</p>
            <p style='margin: 10px 0 0 0; font-size: 14px; font-weight: 600;'>— {quote.Author}</p>
        </div>"
            : "";

        // Generate leaderboard table rows
        var leaderboardRows = string.Join("", leaderboard.Take(10).Select(l => 
            $@"<tr style='{(l.Position <= 3 ? "background-color: #fef3c7;" : "")}'>
                <td style='padding: 12px; text-align: center; font-weight: {(l.Position == 1 ? "bold" : "normal")}; color: {(l.Position == 1 ? "#d97706" : "#374151")};'>{l.Position}{GetPositionSuffix(l.Position)}</td>
                <td style='padding: 12px; font-weight: {(l.Position == 1 ? "bold" : "normal")}; color: {(l.Position == 1 ? "#d97706" : "#374151")};'>{l.FullName} (@{l.Username})</td>
                <td style='padding: 12px; text-align: right; font-weight: {(l.Position == 1 ? "bold" : "normal")}; color: {(l.Position == 1 ? "#d97706" : "#374151")};'>{l.Total:F2}</td>
            </tr>"));

        var winnerCongrats = isWinner 
            ? "<div style='background-color: #fef3c7; border: 2px solid #d97706; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: center;'><h2 style='color: #d97706; margin: 0;'>🎉 Congratulations! You Won! 🥇</h2><p style='margin: 10px 0 0 0; color: #92400e;'>You finished in 1st place out of {totalParticipants} participants!</p></div>"
            : $"<div style='background-color: #f3f4f6; border: 1px solid #d1d5db; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: center;'><h2 style='color: #374151; margin: 0;'>Great Job! 🎯</h2><p style='margin: 10px 0 0 0; color: #6b7280;'>You finished in {position}{positionSuffix} place out of {totalParticipants} participants!</p></div>";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Challenge Complete - Final Results</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 700px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #059669; color: white; padding: 25px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .leaderboard {{ background-color: white; border-radius: 8px; overflow: hidden; margin: 20px 0; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }}
        .leaderboard table {{ width: 100%; border-collapse: collapse; }}
        .leaderboard th {{ background-color: #059669; color: white; padding: 15px; text-align: left; font-weight: 600; }}
        .leaderboard th:first-child {{ text-align: center; }}
        .leaderboard th:last-child {{ text-align: right; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🏁 Challenge Complete!</h1>
        <h2 style='margin: 10px 0 0 0; opacity: 0.9;'>{challengeTitle}</h2>
    </div>
    <div class='content'>
        <p>Hello {participantName}!</p>
        <p>The challenge <strong>{challengeTitle}</strong> has officially ended! Here are the final results:</p>
        
        {winnerCongrats}

        <div class='leaderboard'>
            <table>
                <thead>
                    <tr>
                        <th>Position</th>
                        <th>Participant</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {leaderboardRows}
                    {(leaderboard.Count > 10 ? $"<tr><td colspan='3' style='padding: 15px; text-align: center; color: #6b7280; font-style: italic;'>... and {leaderboard.Count - 10} more participants</td></tr>" : "")}
                </tbody>
            </table>
        </div>

        <div style='background-color: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #059669;'>
            <h3>🏆 Challenge Winner</h3>
            <p style='margin: 0; font-size: 18px;'><strong>{winner.FullName}</strong> (@{winner.Username}) with <strong>{winner.Total:F2}</strong> total!</p>
        </div>

        <p>Thank you for participating and pushing your limits! Keep up the fantastic work and stay active. 💪</p>
        
        <div class='footer'>
            <p>You're receiving this email because you participated in this challenge.</p>
            <p>To manage your notification preferences, visit your settings page.</p>
        </div>
        {quoteSection}
    </div>
</body>
</html>";

        var quoteText = quote != null ? $@"

""{quote.Text}""
— {quote.Author}" : "";

        var leaderboardText = string.Join("\n", leaderboard.Take(10).Select(l => 
            $"{l.Position}{GetPositionSuffix(l.Position)}: {l.FullName} (@{l.Username}) - {l.Total:F2}"));

        var textBody = $@"
🏁 Challenge Complete: {challengeTitle}

Hello {participantName}!

The challenge {challengeTitle} has officially ended! Here are the final results:

{(isWinner ? $"🎉 CONGRATULATIONS! YOU WON! 🥇\nYou finished in 1st place out of {totalParticipants} participants!" : $"Great Job! 🎯\nYou finished in {position}{positionSuffix} place out of {totalParticipants} participants!")}

📊 FINAL LEADERBOARD (Top {Math.Min(10, leaderboard.Count)}):
{leaderboardText}
{(leaderboard.Count > 10 ? $"... and {leaderboard.Count - 10} more participants" : "")}

🏆 Challenge Winner: {winner.FullName} (@{winner.Username}) with {winner.Total:F2} total!

Thank you for participating and pushing your limits! Keep up the fantastic work and stay active. 💪

You're receiving this email because you participated in this challenge.
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

    private static string GetPositionSuffix(int position)
    {
        return position switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ when position >= 11 && position <= 13 => "th",
            _ when position % 10 == 1 => "st",
            _ when position % 10 == 2 => "nd",
            _ when position % 10 == 3 => "rd",
            _ => "th"
        };
    }
}