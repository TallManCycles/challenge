using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using backend.Models;
using backend.Services;

namespace backend.Tests.Services;

[TestFixture]
public class MockEmailServiceTests
{
    private Mock<ILogger<MockEmailService>> _mockLogger = null!;
    private Mock<IQuoteService> _mockQuoteService = null!;
    private MockEmailService _emailService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<MockEmailService>>();
        _mockQuoteService = new Mock<IQuoteService>();
        _emailService = new MockEmailService(_mockLogger.Object, _mockQuoteService.Object);
    }

    [Test]
    public async Task SendEmailAsync_LogsEmailDetails()
    {
        // Arrange
        var toEmail = "test@example.com";
        var subject = "Test Subject";
        var htmlBody = "<p>Test HTML Body</p>";
        var textBody = "Test Text Body";

        // Act
        await _emailService.SendEmailAsync(toEmail, subject, htmlBody, textBody);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mock Email - To: test@example.com, Subject: Test Subject")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mock Email HTML Body: <p>Test HTML Body</p>")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mock Email Text Body: Test Text Body")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_WithQuote_IncludesQuoteInMessage()
    {
        // Arrange
        var quote = new Quote
        {
            Text = "It never gets easier, you just go faster.",
            Author = "Greg LeMond",
            Category = "Training"
        };

        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ReturnsAsync(quote);

        var toEmail = "participant@example.com";
        var userName = "TestUser";
        var activityName = "Morning Ride";
        var challengeTitle = "100 Mile Challenge";
        var activityValue = 25.5m;
        var challengeType = "Distance";

        // Act
        await _emailService.SendChallengeActivityNotificationAsync(
            toEmail, userName, activityName, challengeTitle, activityValue, challengeType);

        // Assert
        _mockQuoteService.Verify(x => x.GetRandomQuoteAsync(), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mock Email HTML Body: TestUser completed Morning Ride with 25.5 km in challenge 100 Mile Challenge | Quote: \"It never gets easier, you just go faster.\" - Greg LeMond")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_WithoutQuote_DoesNotIncludeQuoteInMessage()
    {
        // Arrange
        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ReturnsAsync((Quote?)null);

        var toEmail = "participant@example.com";
        var userName = "TestUser";
        var activityName = "Morning Ride";
        var challengeTitle = "100 Mile Challenge";
        var activityValue = 25.5m;
        var challengeType = "Distance";

        // Act
        await _emailService.SendChallengeActivityNotificationAsync(
            toEmail, userName, activityName, challengeTitle, activityValue, challengeType);

        // Assert
        _mockQuoteService.Verify(x => x.GetRandomQuoteAsync(), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mock Email HTML Body: TestUser completed Morning Ride with 25.5 km in challenge 100 Mile Challenge") && !v.ToString()!.Contains("Quote:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_CorrectUnitForChallengeType()
    {
        // Arrange
        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ReturnsAsync((Quote?)null);

        var testCases = new[]
        {
            new { ChallengeType = "Distance", ExpectedUnit = "km" },
            new { ChallengeType = "Elevation", ExpectedUnit = "m" },
            new { ChallengeType = "Time", ExpectedUnit = "hours" },
            new { ChallengeType = "Unknown", ExpectedUnit = "units" }
        };

        foreach (var testCase in testCases)
        {
            // Reset mock between iterations to avoid cross-contamination
            _mockLogger.Reset();
            
            // Act
            await _emailService.SendChallengeActivityNotificationAsync(
                "test@example.com", "User", "Activity", "Challenge", 100m, testCase.ChallengeType);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Mock Email HTML Body: User completed Activity with 100 {testCase.ExpectedUnit} in challenge Challenge")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}

[TestFixture]
public class AwsSesEmailServiceTests
{
    private Mock<Amazon.SimpleEmail.IAmazonSimpleEmailService> _mockSesClient = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ILogger<AwsSesEmailService>> _mockLogger = null!;
    private Mock<IQuoteService> _mockQuoteService = null!;
    private AwsSesEmailService _emailService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSesClient = new Mock<Amazon.SimpleEmail.IAmazonSimpleEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AwsSesEmailService>>();
        _mockQuoteService = new Mock<IQuoteService>();

        _mockConfiguration.Setup(x => x["Email:FromAddress"])
            .Returns("noreply@test.com");

        _emailService = new AwsSesEmailService(
            _mockSesClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockQuoteService.Object);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_WithQuote_IncludesQuoteInHtmlAndText()
    {
        // Arrange
        var quote = new Quote
        {
            Text = "The race is won by the rider who can suffer the most.",
            Author = "Eddy Merckx",
            Category = "Training"
        };

        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ReturnsAsync(quote);

        _mockSesClient.Setup(x => x.SendEmailAsync(It.IsAny<Amazon.SimpleEmail.Model.SendEmailRequest>(), default))
            .ReturnsAsync(new Amazon.SimpleEmail.Model.SendEmailResponse
            {
                MessageId = "test-message-id"
            });

        // Act
        await _emailService.SendChallengeActivityNotificationAsync(
            "test@example.com", "TestUser", "Morning Ride", "100 Mile Challenge", 25.5m, "Distance");

        // Assert
        _mockQuoteService.Verify(x => x.GetRandomQuoteAsync(), Times.Once);

        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<Amazon.SimpleEmail.Model.SendEmailRequest>(req =>
                req.Message.Body.Html.Data.Contains("The race is won by the rider who can suffer the most.") &&
                req.Message.Body.Html.Data.Contains("— Eddy Merckx") &&
                req.Message.Body.Text.Data.Contains("\"The race is won by the rider who can suffer the most.\"") &&
                req.Message.Body.Text.Data.Contains("— Eddy Merckx")),
            default), Times.Once);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_WithoutQuote_DoesNotIncludeQuoteSection()
    {
        // Arrange
        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ReturnsAsync((Quote?)null);

        _mockSesClient.Setup(x => x.SendEmailAsync(It.IsAny<Amazon.SimpleEmail.Model.SendEmailRequest>(), default))
            .ReturnsAsync(new Amazon.SimpleEmail.Model.SendEmailResponse
            {
                MessageId = "test-message-id"
            });

        // Act
        await _emailService.SendChallengeActivityNotificationAsync(
            "test@example.com", "TestUser", "Morning Ride", "100 Mile Challenge", 25.5m, "Distance");

        // Assert
        _mockQuoteService.Verify(x => x.GetRandomQuoteAsync(), Times.Once);

        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<Amazon.SimpleEmail.Model.SendEmailRequest>(req =>
                !req.Message.Body.Html.Data.Contains("border-top: 1px solid #e5e7eb") && // Quote section styling
                !req.Message.Body.Text.Data.Contains("—")), // Quote author attribution
            default), Times.Once);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_QuoteServiceThrows_ContinuesWithoutQuote()
    {
        // Arrange
        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ThrowsAsync(new Exception("Quote service error"));

        _mockSesClient.Setup(x => x.SendEmailAsync(It.IsAny<Amazon.SimpleEmail.Model.SendEmailRequest>(), default))
            .ReturnsAsync(new Amazon.SimpleEmail.Model.SendEmailResponse
            {
                MessageId = "test-message-id"
            });

        // Act
        await _emailService.SendChallengeActivityNotificationAsync(
            "test@example.com", "TestUser", "Morning Ride", "100 Mile Challenge", 25.5m, "Distance");

        // Assert - Should continue and send email without quote
        _mockSesClient.Verify(x => x.SendEmailAsync(It.IsAny<Amazon.SimpleEmail.Model.SendEmailRequest>(), default), Times.Once);
        
        // Verify that a warning was logged about the quote failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve quote for email notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendChallengeActivityNotificationAsync_CorrectEmailStructure()
    {
        // Arrange
        var quote = new Quote
        {
            Text = "Test quote",
            Author = "Test Author",
            Category = "Test"
        };

        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
            .ReturnsAsync(quote);

        _mockSesClient.Setup(x => x.SendEmailAsync(It.IsAny<Amazon.SimpleEmail.Model.SendEmailRequest>(), default))
            .ReturnsAsync(new Amazon.SimpleEmail.Model.SendEmailResponse
            {
                MessageId = "test-message-id"
            });

        // Act
        await _emailService.SendChallengeActivityNotificationAsync(
            "test@example.com", "TestUser", "Morning Ride", "100 Mile Challenge", 25.5m, "Distance");

        // Assert
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<Amazon.SimpleEmail.Model.SendEmailRequest>(req =>
                req.Source == "noreply@test.com" &&
                req.Destination.ToAddresses.Contains("test@example.com") &&
                req.Message.Subject.Data == "New Activity in Challenge: 100 Mile Challenge" &&
                req.Message.Body.Html.Data.Contains("TestUser") &&
                req.Message.Body.Html.Data.Contains("Morning Ride") &&
                req.Message.Body.Html.Data.Contains("100 Mile Challenge") &&
                req.Message.Body.Html.Data.Contains("25.50 km") &&
                req.Message.Body.Text.Data.Contains("TestUser") &&
                req.Message.Body.Text.Data.Contains("Morning Ride")),
            default), Times.Once);
    }
}