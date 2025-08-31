using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace backend.Tests.Services;

[TestFixture]
public class FitFileNotificationServiceTests
{
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<FitFileReprocessingService>> _mockLogger = null!;
    private Mock<IChallengeNotificationService> _mockNotificationService = null!;
    private FitFileReprocessingService _reprocessingService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<FitFileReprocessingService>>();
        _mockNotificationService = new Mock<IChallengeNotificationService>();
        _reprocessingService = new FitFileReprocessingService(_context, _mockLogger.Object, _mockNotificationService.Object);
    }

    [Test]
    public async Task ReprocessUnprocessedFitFilesAsync_WithValidUser_CallsNotificationService()
    {
        // Arrange
        var testUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            ZwiftUserId = "123456789",
            EmailNotificationsEnabled = true
        };
        
        var fitFileActivity = new FitFileActivity
        {
            Id = 1,
            FileName = "test-activity.fit",
            ZwiftUserId = "123456789",
            Status = FitFileProcessingStatus.Unprocessed,
            ActivityName = "Test Ride",
            ActivityType = "cycling",
            DistanceKm = 25.5,
            DurationMinutes = 60,
            ElevationGainM = 250,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            ActivityDate = DateTime.UtcNow,
            ProcessedAt = null
        };
        
        _context.Users.Add(testUser);
        _context.FitFileActivities.Add(fitFileActivity);
        await _context.SaveChangesAsync();

        // Setup notification service
        _mockNotificationService
            .Setup(x => x.SendActivityNotificationsForAllChallengesAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _reprocessingService.ReprocessUnprocessedFitFilesAsync(testUser.Id);

        // Assert
        Assert.AreEqual(1, result, "Should reprocess 1 FIT file");
        
        // Verify that activity notifications were called
        _mockNotificationService.Verify(
            x => x.SendActivityNotificationsForAllChallengesAsync(It.IsAny<int>()), 
            Times.Once, 
            "Activity notifications should be sent when reprocessing creates an activity");

        // Verify an Activity was created
        var activities = await _context.Activities.Where(a => a.UserId == testUser.Id).ToListAsync();
        Assert.AreEqual(1, activities.Count, "One activity should be created from reprocessing");
        Assert.AreEqual("FitFile", activities.First().Source, "Activity source should be marked as FitFile");
        
        // Verify FitFileActivity status was updated
        var updatedFitFile = await _context.FitFileActivities.FirstAsync();
        Assert.AreEqual(FitFileProcessingStatus.Processed, updatedFitFile.Status);
        Assert.AreEqual(testUser.Id, updatedFitFile.UserId);
    }

    [Test]
    public async Task ReprocessUnprocessedFitFilesAsync_NoUser_DoesNotCallNotificationService()
    {
        // Arrange
        var fitFileActivity = new FitFileActivity
        {
            Id = 1,
            FileName = "test-activity.fit",
            ZwiftUserId = "999999999", // Non-existent user
            Status = FitFileProcessingStatus.Unprocessed,
            ActivityName = "Test Ride",
            ActivityType = "cycling",
            DistanceKm = 25.5,
            DurationMinutes = 60,
            ElevationGainM = 250,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            ActivityDate = DateTime.UtcNow,
            ProcessedAt = null
        };
        
        _context.FitFileActivities.Add(fitFileActivity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reprocessingService.ReprocessUnprocessedFitFilesAsync(999); // Non-existent user ID

        // Assert
        Assert.AreEqual(0, result, "Should reprocess 0 FIT files when user not found");
        
        // Verify that NO activity notifications were called
        _mockNotificationService.Verify(
            x => x.SendActivityNotificationsForAllChallengesAsync(It.IsAny<int>()), 
            Times.Never, 
            "Activity notifications should NOT be sent when user not found");

        // Verify no Activity was created
        var activities = await _context.Activities.ToListAsync();
        Assert.AreEqual(0, activities.Count, "No activities should be created when user not found");
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }
}