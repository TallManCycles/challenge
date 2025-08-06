using NUnit.Framework;
using backend.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Moq;

namespace backend.Tests;

[TestFixture]
public class FileLoggingServiceTests
{
    private FileLoggingService _loggingService;
    private string _testLogDirectory;

    [SetUp]
    public void Setup()
    {
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.SetupGet(env => env.EnvironmentName).Returns("Production");
        
        _loggingService = new FileLoggingService(mockEnvironment.Object);
        _testLogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test log files after each test
        if (Directory.Exists(_testLogDirectory))
        {
            var files = Directory.GetFiles(_testLogDirectory, "*.log");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Test]
    public async Task LogAsync_ShouldCreateLogFileAndWriteEntry()
    {
        // Arrange
        var message = "Test log message";
        var level = "INFO";
        var category = "TestCategory";

        // Act
        await _loggingService.LogAsync(level, message, category);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        Assert.That(File.Exists(expectedFilePath), Is.True, "Log file should be created");
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Is.Not.Empty, "Log file should contain content");
        Assert.That(logContent, Does.Contain(message), "Log should contain the message");
        Assert.That(logContent, Does.Contain(level), "Log should contain the level");
        Assert.That(logContent, Does.Contain(category), "Log should contain the category");
    }

    [Test]
    public async Task LogInfoAsync_ShouldWriteInfoLevelLog()
    {
        // Arrange
        var message = "Info message";
        var category = "InfoCategory";

        // Act
        await _loggingService.LogInfoAsync(message, category);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        Assert.That(File.Exists(expectedFilePath), Is.True);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Does.Contain("\"Level\": \"INFO\""));
        Assert.That(logContent, Does.Contain(message));
        Assert.That(logContent, Does.Contain(category));
    }

    [Test]
    public async Task LogWarningAsync_ShouldWriteWarningLevelLog()
    {
        // Arrange
        var message = "Warning message";

        // Act
        await _loggingService.LogWarningAsync(message);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        Assert.That(File.Exists(expectedFilePath), Is.True);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Does.Contain("\"Level\": \"WARNING\""));
        Assert.That(logContent, Does.Contain(message));
    }

    [Test]
    public async Task LogErrorAsync_ShouldWriteErrorLevelLogWithException()
    {
        // Arrange
        var message = "Error message";
        var exception = new InvalidOperationException("Test exception");
        var category = "ErrorCategory";

        // Act
        await _loggingService.LogErrorAsync(message, exception, category);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        Assert.That(File.Exists(expectedFilePath), Is.True);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Does.Contain("\"Level\": \"ERROR\""));
        Assert.That(logContent, Does.Contain(message));
        Assert.That(logContent, Does.Contain(category));
        Assert.That(logContent, Does.Contain("Test exception"));
    }

    [Test]
    public async Task LogDebugAsync_ShouldWriteDebugLevelLog()
    {
        // Arrange
        var message = "Debug message";

        // Act
        await _loggingService.LogDebugAsync(message);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        Assert.That(File.Exists(expectedFilePath), Is.True);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Does.Contain("\"Level\": \"DEBUG\""));
        Assert.That(logContent, Does.Contain(message));
    }

    [Test]
    public async Task LogAsync_WithNullCategory_ShouldUseGeneralCategory()
    {
        // Arrange
        var message = "Test message with null category";

        // Act
        await _loggingService.LogAsync("INFO", message);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Does.Contain("\"Category\": \"General\""));
    }

    [Test]
    public async Task LogAsync_MultipleEntries_ShouldAppendToSameFile()
    {
        // Arrange
        var message1 = "First message";
        var message2 = "Second message";

        // Act
        await _loggingService.LogInfoAsync(message1);
        await _loggingService.LogWarningAsync(message2);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        Assert.That(logContent, Does.Contain(message1));
        Assert.That(logContent, Does.Contain(message2));
        Assert.That(logContent, Does.Contain("\"Level\": \"INFO\""));
        Assert.That(logContent, Does.Contain("\"Level\": \"WARNING\""));
    }

    [Test]
    public async Task LogAsync_ShouldCreateValidJsonEntries()
    {
        // Arrange
        var message = "JSON validation test";

        // Act
        await _loggingService.LogInfoAsync(message);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        var entries = logContent.Split(new[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        
        Assert.That(entries.Length, Is.GreaterThan(0));
        
        // Try to deserialize the first entry to validate JSON format
        var firstEntry = entries[0].Trim();
        Assert.DoesNotThrow(() => JsonSerializer.Deserialize<JsonElement>(firstEntry));
    }

    [Test]
    public async Task LogAsync_ConcurrentCalls_ShouldHandleThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();
        var messageCount = 10;

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var index = i;
            tasks.Add(_loggingService.LogInfoAsync($"Concurrent message {index}"));
        }

        await Task.WhenAll(tasks);

        // Assert
        var expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(_testLogDirectory, expectedFileName);
        
        Assert.That(File.Exists(expectedFilePath), Is.True);
        
        var logContent = await File.ReadAllTextAsync(expectedFilePath);
        
        // Verify all messages are present
        for (int i = 0; i < messageCount; i++)
        {
            Assert.That(logContent, Does.Contain($"Concurrent message {i}"));
        }
    }
}