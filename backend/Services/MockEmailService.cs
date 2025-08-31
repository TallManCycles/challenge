using backend.Models;

namespace backend.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    private readonly IQuoteService _quoteService;

    public MockEmailService(ILogger<MockEmailService> logger, IQuoteService quoteService)
    {
        _logger = logger;
        _quoteService = quoteService;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        _logger.LogInformation("Mock Email - To: {Email}, Subject: {Subject}", toEmail, subject);
        _logger.LogInformation("Mock Email HTML Body: {HtmlBody}", htmlBody);
        if (!string.IsNullOrEmpty(textBody))
        {
            _logger.LogInformation("Mock Email Text Body: {TextBody}", textBody);
        }
        
        // Simulate async email sending
        await Task.Delay(100);
    }

    public async Task SendChallengeActivityNotificationAsync(string toEmail, string userName, string activityName, string challengeTitle, decimal activityValue, string challengeType)
    {
        _logger.LogInformation("Mock Challenge Notification - To: {Email}, User: {User}, Activity: {Activity}, Challenge: {Challenge}, Value: {Value} {Type}", 
            toEmail, userName, activityName, challengeTitle, activityValue, challengeType);
        
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
        
        var quoteText = quote != null ? $" | Quote: \"{quote.Text}\" - {quote.Author}" : "";
        
        var subject = $"New Activity in Challenge: {challengeTitle}";
        var message = $"{userName} completed {activityName} with {activityValue} {GetUnitForChallengeType(challengeType)} in challenge {challengeTitle}{quoteText}";
        
        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendChallengeCompletionNotificationAsync(string toEmail, string challengeTitle, string participantName, int position, int totalParticipants, List<(string Username, string FullName, decimal Total, int Position)> leaderboard)
    {
        var winner = leaderboard.FirstOrDefault();
        var positionSuffix = GetPositionSuffix(position);
        
        _logger.LogInformation("Mock Challenge Completion Notification - To: {Email}, Challenge: {Challenge}, Participant: {Participant}, Position: {Position}{Suffix} of {Total}", 
            toEmail, challengeTitle, participantName, position, positionSuffix, totalParticipants);
        
        if (winner.Username != null)
        {
            _logger.LogInformation("Mock Challenge Winner: {WinnerName} (@{WinnerUsername}) with {WinnerTotal:F2}", 
                winner.FullName, winner.Username, winner.Total);
        }
        
        _logger.LogInformation("Mock Leaderboard (Top {Count}):", Math.Min(5, leaderboard.Count));
        foreach (var participant in leaderboard.Take(5))
        {
            _logger.LogInformation("  {Position}{Suffix}: {FullName} (@{Username}) - {Total:F2}", 
                participant.Position, GetPositionSuffix(participant.Position), participant.FullName, participant.Username, participant.Total);
        }
        
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
        
        var quoteText = quote != null ? $" | Quote: \"{quote.Text}\" - {quote.Author}" : "";
        
        var subject = $"Challenge Complete: {challengeTitle} - Final Results!";
        var isWinner = position == 1;
        var message = $"Challenge {challengeTitle} finished! {participantName} finished {position}{positionSuffix} of {totalParticipants}. " +
                     $"{(isWinner ? "🎉 WINNER! 🥇" : "Great job!")} Winner: {winner.FullName} ({winner.Total:F2}){quoteText}";
        
        await SendEmailAsync(toEmail, subject, message);
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