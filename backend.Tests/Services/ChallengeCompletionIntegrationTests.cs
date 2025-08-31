using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Tests.Services;

[TestFixture]
public class ChallengeCompletionIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<MockEmailService>> _mockEmailLogger = null!;
    private Mock<IQuoteService> _mockQuoteService = null!;
    private MockEmailService _mockEmailService = null!;
    private User _user1 = null!;
    private User _user2 = null!;
    private User _user3 = null!;

    [SetUp]
    public void SetUp()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup email service
        _mockEmailLogger = new Mock<ILogger<MockEmailService>>();
        _mockQuoteService = new Mock<IQuoteService>();
        _mockQuoteService.Setup(x => x.GetRandomQuoteAsync())
                        .ReturnsAsync(new Quote 
                        { 
                            Text = "Test Quote", 
                            Author = "Test Author", 
                            Category = "Test", 
                            IsActive = true 
                        });
        
        _mockEmailService = new MockEmailService(_mockEmailLogger.Object, _mockQuoteService.Object);

        // Create test users
        _user1 = new User
        {
            Id = 1,
            Username = "winner",
            Email = "winner@test.com",
            FullName = "Winner User",
            EmailNotificationsEnabled = true
        };

        _user2 = new User
        {
            Id = 2,
            Username = "runner_up",
            Email = "runnerup@test.com",
            FullName = "Runner Up User",
            EmailNotificationsEnabled = true
        };

        _user3 = new User
        {
            Id = 3,
            Username = "third_place",
            Email = "third@test.com",
            FullName = "Third Place User",
            EmailNotificationsEnabled = false // Notifications disabled
        };

        _context.Users.AddRange(_user1, _user2, _user3);
        _context.SaveChanges();
    }

    [Test]
    public async Task MockEmailService_SendChallengeCompletionNotification_LogsCorrectWinnerInformation()
    {
        // Arrange
        var challengeTitle = "Test Challenge";
        var participantName = "Winner User";
        var position = 1;
        var totalParticipants = 2;
        var leaderboard = new List<(string Username, string FullName, decimal Total, int Position)>
        {
            ("winner", "Winner User", 100.5m, 1),
            ("runner_up", "Runner Up User", 75.2m, 2)
        };

        // Act
        await _mockEmailService.SendChallengeCompletionNotificationAsync(
            _user1.Email,
            challengeTitle,
            participantName,
            position,
            totalParticipants,
            leaderboard
        );

        // Assert - Verify winner notification was logged
        _mockEmailLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("🎉 WINNER! 🥇")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify leaderboard information was logged
        _mockEmailLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1st: Winner User (@winner) - 100.50")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task MockEmailService_SendChallengeCompletionNotification_LogsCorrectRunnerUpInformation()
    {
        // Arrange
        var challengeTitle = "Test Challenge";
        var participantName = "Runner Up User";
        var position = 2;
        var totalParticipants = 2;
        var leaderboard = new List<(string Username, string FullName, decimal Total, int Position)>
        {
            ("winner", "Winner User", 100.5m, 1),
            ("runner_up", "Runner Up User", 75.2m, 2)
        };

        // Act
        await _mockEmailService.SendChallengeCompletionNotificationAsync(
            _user2.Email,
            challengeTitle,
            participantName,
            position,
            totalParticipants,
            leaderboard
        );

        // Assert - Verify non-winner notification was logged
        _mockEmailLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Great job!") && !v.ToString()!.Contains("🎉 WINNER! 🥇")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ChallengeCompletion_DatabaseIntegration_FindsCompletedChallenge()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var challenge = new Challenge
        {
            Id = 1,
            Title = "Completed Challenge",
            Description = "A completed test challenge",
            CreatedById = _user1.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = yesterday.AddHours(23).AddMinutes(59), // Ended yesterday
            IsActive = true
        };

        var participants = new List<ChallengeParticipant>
        {
            new ChallengeParticipant
            {
                Id = 1,
                ChallengeId = 1,
                UserId = _user1.Id,
                CurrentTotal = 100.5m,
                User = _user1
            },
            new ChallengeParticipant
            {
                Id = 2,
                ChallengeId = 1,
                UserId = _user2.Id,
                CurrentTotal = 75.2m,
                User = _user2
            }
        };

        challenge.Participants = participants;
        _context.Challenges.Add(challenge);
        _context.ChallengeParticipants.AddRange(participants);
        await _context.SaveChangesAsync();

        // Act - Query for completed challenges (same logic as in ChallengeNotificationService)
        var yesterdayStart = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        var completedChallenges = await _context.Challenges
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.IsActive && 
                       c.EndDate >= yesterdayStart && 
                       c.EndDate < today)
            .ToListAsync();

        // Assert
        Assert.AreEqual(1, completedChallenges.Count);
        Assert.AreEqual("Completed Challenge", completedChallenges.First().Title);
        
        var leaderboard = completedChallenges.First().Participants
            .Where(p => p.User.EmailNotificationsEnabled)
            .OrderByDescending(p => p.CurrentTotal)
            .ToList();
            
        Assert.AreEqual(2, leaderboard.Count);
        Assert.AreEqual("winner", leaderboard.First().User.Username);
        Assert.AreEqual(100.5m, leaderboard.First().CurrentTotal);
    }

    [Test]
    public async Task ChallengeCompletion_LeaderboardOrdering_SortsParticipantsByTotal()
    {
        // Arrange
        var challenge = new Challenge
        {
            Id = 1,
            Title = "Test Challenge",
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };

        var participants = new List<ChallengeParticipant>
        {
            new ChallengeParticipant { UserId = _user1.Id, CurrentTotal = 75.0m, User = _user1 },
            new ChallengeParticipant { UserId = _user2.Id, CurrentTotal = 100.5m, User = _user2 },  // Should be winner
            new ChallengeParticipant { UserId = _user3.Id, CurrentTotal = 50.0m, User = _user3 }
        };

        challenge.Participants = participants;
        _context.Challenges.Add(challenge);
        _context.ChallengeParticipants.AddRange(participants);
        await _context.SaveChangesAsync();

        // Act - Generate leaderboard (same logic as in ChallengeNotificationService)
        var leaderboard = challenge.Participants
            .OrderByDescending(p => p.CurrentTotal)
            .Select((p, index) => new
            {
                Username = p.User.Username,
                FullName = p.User.FullName,
                Total = p.CurrentTotal,
                Position = index + 1,
                User = p.User
            })
            .ToList();

        // Assert
        Assert.AreEqual(3, leaderboard.Count);
        
        // Winner should be user2 with 100.5
        Assert.AreEqual("runner_up", leaderboard[0].Username);
        Assert.AreEqual(100.5m, leaderboard[0].Total);
        Assert.AreEqual(1, leaderboard[0].Position);
        
        // Second should be user1 with 75.0
        Assert.AreEqual("winner", leaderboard[1].Username);
        Assert.AreEqual(75.0m, leaderboard[1].Total);
        Assert.AreEqual(2, leaderboard[1].Position);
        
        // Third should be user3 with 50.0
        Assert.AreEqual("third_place", leaderboard[2].Username);
        Assert.AreEqual(50.0m, leaderboard[2].Total);
        Assert.AreEqual(3, leaderboard[2].Position);
    }

    [Test]
    public void ChallengeCompletion_OnlyNotificationsEnabledUsers_FiltersCorrectly()
    {
        // Arrange
        var participants = new List<ChallengeParticipant>
        {
            new ChallengeParticipant { UserId = _user1.Id, User = _user1 }, // Notifications enabled
            new ChallengeParticipant { UserId = _user2.Id, User = _user2 }, // Notifications enabled
            new ChallengeParticipant { UserId = _user3.Id, User = _user3 }  // Notifications disabled
        };

        // Act - Filter participants with email notifications enabled
        var notificationTargets = participants
            .Where(p => p.User.EmailNotificationsEnabled)
            .ToList();

        // Assert
        Assert.AreEqual(2, notificationTargets.Count);
        Assert.IsTrue(notificationTargets.All(p => p.User.EmailNotificationsEnabled));
        Assert.IsTrue(notificationTargets.Any(p => p.User.Username == "winner"));
        Assert.IsTrue(notificationTargets.Any(p => p.User.Username == "runner_up"));
        Assert.IsFalse(notificationTargets.Any(p => p.User.Username == "third_place"));
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }
}