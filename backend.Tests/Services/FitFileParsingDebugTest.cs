using System;
using System.IO;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace backend.Tests.Services;

[TestFixture]
public class FitFileParsingDebugTest
{
    private ApplicationDbContext _context;
    private Mock<ILogger<FitFileProcessingService>> _mockLogger;
    private FitFileProcessingService _fitFileProcessingService;
    private readonly string _testFitFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test-fit-file", "20250810_095144_2024-12-02-05-14-35.fit");

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<FitFileProcessingService>>();
        _fitFileProcessingService = new FitFileProcessingService(_context, _mockLogger.Object);
    }

    [Test]
    public void TestFitFileExists()
    {
        var exists = File.Exists(_testFitFilePath);
        Console.WriteLine($"FIT file exists: {exists}");
        Console.WriteLine($"File path: {_testFitFilePath}");
        
        if (exists)
        {
            var fileInfo = new FileInfo(_testFitFilePath);
            Console.WriteLine($"File size: {fileInfo.Length} bytes");
            Console.WriteLine($"Last modified: {fileInfo.LastWriteTime}");
        }
        
        Assert.That(exists, Is.True, "Test FIT file should exist");
    }

    [Test] 
    public async Task TestFitFileParsing_BasicTest()
    {
        // Arrange
        var fileName = "20250810_095144_2024-12-02-05-14-35.fit";
        
        // Create test user with the actual ZwiftUserId from the FIT file
        var testUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com", 
            PasswordHash = "hash",
            ZwiftUserId = "3825981698" // Actual ZwiftUserId extracted from the FIT file
        };
        
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Test if file exists first
        var fileExists = File.Exists(_testFitFilePath);
        Console.WriteLine($"File exists: {fileExists}");
        
        if (!fileExists)
        {
            Assert.Fail($"Test file does not exist at: {_testFitFilePath}");
            return;
        }

        var fileContent = await File.ReadAllBytesAsync(_testFitFilePath);

        // Act
        Console.WriteLine("Starting FIT file processing...");
        var result = await _fitFileProcessingService.ProcessFitFileAsync(fileContent, fileName);
        
        Console.WriteLine($"Processing result: {result}");
        
        // Check if any activities were created
        var activities = await _context.Activities.ToListAsync();
        Console.WriteLine($"Activities created: {activities.Count}");
        
        foreach (var activity in activities)
        {
            Console.WriteLine($"Activity: {activity.ActivityName}, Distance: {activity.DistanceKm}km, Duration: {activity.DurationMinutes}min");
        }
        
        // Check all users to see if we have ZwiftUserId issues
        var users = await _context.Users.ToListAsync();
        Console.WriteLine($"Users in database: {users.Count}");
        foreach (var user in users)
        {
            Console.WriteLine($"User: {user.Username}, ZwiftUserId: {user.ZwiftUserId ?? "NULL"}");
        }
        
        // Print all log calls to see what happened
        Console.WriteLine("\n--- Log Messages ---");
        foreach (var logCall in _mockLogger.Invocations)
        {
            if (logCall.Arguments.Count >= 3)
            {
                var logLevel = logCall.Arguments[0];
                var message = logCall.Arguments[2];
                Console.WriteLine($"[{logLevel}] {message}");
            }
        }
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }
}