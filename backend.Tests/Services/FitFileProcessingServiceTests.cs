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
public class FitFileProcessingServiceTests
{
    private ApplicationDbContext _context;
    private Mock<ILogger<FitFileProcessingService>> _mockLogger;
    private FitFileProcessingService _fitFileProcessingService;
    // test file is located in /test-fit-file/20250810_095144_2024-12-02-05-14-35.fit
    private readonly string _testFitFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test-fit-file", "20250810_095144_2024-12-02-05-14-35.fit");

    [SetUp]
    public void Setup()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<FitFileProcessingService>>();
        _fitFileProcessingService = new FitFileProcessingService(_context, _mockLogger.Object);
    }

    [Test]
    public async Task ParseFitFileAsync_ShouldParseRealZwiftFile_Successfully()
    {
        // Arrange
        var fileName = "20250810_095144_2024-12-02-05-14-35.fit";
        
        // Create a test user with ZwiftUserId
        var testUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            ZwiftUserId = "3825981698" // This should match what we extract from the FIT file
        };
        
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var fileContent = await File.ReadAllBytesAsync(_testFitFilePath);

        // Act
        var result = await _fitFileProcessingService.ProcessFitFileAsync(fileContent, fileName);

        // Assert
        Assert.True(result, "FIT file processing should succeed");
        
        // Verify that an activity was created
        var activity = await _context.FitFileActivities
            .FirstOrDefaultAsync(a => a.FileName == fileName);
        
        Assert.NotNull(activity);
        Assert.That(activity.UserId, Is.EqualTo(testUser.Id));
        Assert.That(activity.ActivityType, Is.EqualTo("cycling"));
        Assert.That(activity.DistanceKm, Is.GreaterThan(0), "Distance should be greater than 0");
        Assert.That(activity.DurationMinutes, Is.GreaterThan(0), "Duration should be greater than 0");
        Assert.That(activity.ZwiftUserId, Is.EqualTo(testUser.ZwiftUserId));

        // Log the extracted data for verification
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully parsed FIT file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ParseFitFileAsync_ShouldExtractActivityData_WithCorrectValues()
    {
        // Arrange
        var fileName = "20250810_095144_2024-12-02-05-14-35.fit";
        
        // Create a test user
        var testUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            ZwiftUserId = "3825981698"
        };
        
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var fileContent = await File.ReadAllBytesAsync(_testFitFilePath);

        // Act
        var result = await _fitFileProcessingService.ProcessFitFileAsync(fileContent, fileName);

        // Assert
        Assert.True(result);
        
        var activity = await _context.FitFileActivities
            .FirstOrDefaultAsync(a => a.FileName == fileName);
        
        Assert.NotNull(activity);
        
        // Basic validation - these should be reasonable values for a cycling activity
        Assert.That(activity.DistanceKm, Is.GreaterThanOrEqualTo(0));
        Assert.That(activity.DurationMinutes, Is.GreaterThan(0));
        Assert.That(activity.StartTime, Is.Not.EqualTo(default(DateTime)));
        Assert.That(activity.EndTime, Is.GreaterThan(activity.StartTime));
    }

    [Test]
    public async Task ParseFitFileAsync_WithInvalidData_ShouldReturnFalse()
    {
        // Arrange
        var fileName = "invalid.fit";
        var invalidFileContent = new byte[0];

        // Act
        var result = await _fitFileProcessingService.ProcessFitFileAsync(invalidFileContent, fileName);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ParseFitFile_FileExists_ShouldReturnTrue()
    {
        // Arrange & Act
        var fileExists = System.IO.File.Exists(_testFitFilePath);

        // Assert
        Assert.That(fileExists, Is.True, $"Test FIT file should exist at: {_testFitFilePath}");
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }
}