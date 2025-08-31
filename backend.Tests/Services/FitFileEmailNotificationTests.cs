using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.IO;

namespace backend.Tests.Services;

[TestFixture]
public class FitFileEmailNotificationTests
{
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<FitFileProcessingService>> _mockLogger = null!;
    private Mock<IChallengeNotificationService> _mockNotificationService = null!;
    private FitFileProcessingService _fitFileProcessingService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<FitFileProcessingService>>();
        _mockNotificationService = new Mock<IChallengeNotificationService>();
        _fitFileProcessingService = new FitFileProcessingService(_context, _mockLogger.Object, _mockNotificationService.Object);
    }

    [Test]
    public async Task ProcessFitFileAsync_WithValidUser_SendsActivityNotifications()
    {
        // Arrange
        var testUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            ZwiftUserId = "3825981698", // This matches the real FIT file
            EmailNotificationsEnabled = true
        };
        
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Use the real FIT file from the test directory
        var testFitFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test-fit-file", "20250810_095144_2024-12-02-05-14-35.fit");
        var fitFileContent = await File.ReadAllBytesAsync(testFitFilePath);
        var fileName = "20250810_095144_2024-12-02-05-14-35.fit";

        // Setup the notification service to track calls
        _mockNotificationService
            .Setup(x => x.SendActivityNotificationsForAllChallengesAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _fitFileProcessingService.ProcessFitFileAsync(fitFileContent, fileName);

        // Assert
        Assert.IsTrue(result, "FIT file processing should succeed");
        
        // Verify that activity notifications were called
        _mockNotificationService.Verify(
            x => x.SendActivityNotificationsForAllChallengesAsync(It.IsAny<int>()), 
            Times.Once, 
            "Activity notifications should be sent when FIT file creates an activity");

        // Verify an Activity was created
        var activities = await _context.Activities.Where(a => a.UserId == testUser.Id).ToListAsync();
        Assert.AreEqual(1, activities.Count, "One activity should be created from the FIT file");
        Assert.AreEqual("FitFile", activities.First().Source, "Activity source should be marked as FitFile");
    }

    [Test]
    public async Task ProcessFitFileAsync_UserNotFound_DoesNotSendNotifications()
    {
        // Arrange - Use real FIT file but no matching user in database
        var testFitFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test-fit-file", "20250810_095144_2024-12-02-05-14-35.fit");
        var fitFileContent = await File.ReadAllBytesAsync(testFitFilePath);
        var fileName = "20250810_095144_2024-12-02-05-14-35.fit";
        
        // Don't add any user to the database - this will cause "user not found" scenario

        // Act
        var result = await _fitFileProcessingService.ProcessFitFileAsync(fitFileContent, fileName);

        // Assert
        Assert.IsTrue(result, "FIT file processing should succeed even without user");
        
        // Verify that NO activity notifications were called (since no user was found)
        _mockNotificationService.Verify(
            x => x.SendActivityNotificationsForAllChallengesAsync(It.IsAny<int>()), 
            Times.Never, 
            "Activity notifications should NOT be sent when no user is found");

        // Verify no Activity was created (but FitFileActivity should exist)
        var activities = await _context.Activities.ToListAsync();
        Assert.AreEqual(0, activities.Count, "No activities should be created when user is not found");
        
        // But FitFileActivity should be created with UserNotFound status
        var fitFileActivities = await _context.FitFileActivities.ToListAsync();
        Assert.AreEqual(1, fitFileActivities.Count, "FitFileActivity should be created even when user not found");
        Assert.AreEqual(FitFileProcessingStatus.UserNotFound, fitFileActivities.First().Status);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }
}