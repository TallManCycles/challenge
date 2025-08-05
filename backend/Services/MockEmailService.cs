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