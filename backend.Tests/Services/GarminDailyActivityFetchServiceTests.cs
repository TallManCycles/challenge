using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Tests.Services;

public class GarminDailyActivityFetchServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IFileLoggingService> _mockLogger;
    private readonly Mock<IGarminActivityProcessingService> _mockActivityProcessingService;
    private readonly Mock<IOptions<GarminOAuthConfig>> _mockConfig;
    private readonly GarminDailyActivityFetchService _service;

    public GarminDailyActivityFetchServiceTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<IFileLoggingService>();
        _mockActivityProcessingService = new Mock<IGarminActivityProcessingService>();
        _mockConfig = new Mock<IOptions<GarminOAuthConfig>>();

        // Setup config
        var config = new GarminOAuthConfig
        {
            ConsumerKey = "test-key",
            ConsumerSecret = "test-secret"
        };
        _mockConfig.Setup(x => x.Value).Returns(config);

        _service = new GarminDailyActivityFetchService(
            _mockScopeFactory.Object,
            _mockHttpClientFactory.Object,
            _mockLogger.Object,
            _mockActivityProcessingService.Object,
            _mockConfig.Object);
    }

    [Test]
    public void Service_Should_Be_Created_Successfully()
    {
        // Test that the service can be instantiated without errors
        Assert.That(_service, Is.Not.Null);
    }

    [Test]
    public async Task FetchActivitiesForAllUsersAsync_Should_Complete_Without_Exception()
    {
        // Arrange
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockContext = GetInMemoryDbContext();

        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext))).Returns(mockContext);
        mockServiceProvider.Setup(x => x.GetService(typeof(IFileLoggingService))).Returns(_mockLogger.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        // Act & Assert - Should not throw
        await _service.FetchActivitiesForAllUsersAsync();
        
        // Verify that logging occurred
        _mockLogger.Verify(
            x => x.LogInfoAsync(It.Is<string>(s => s == "Starting daily activity fetch for all users"), null),
            Times.Once);
    }

    [Test]
    public async Task FetchActivitiesForUserAsync_Should_Handle_Missing_Token_Gracefully()
    {
        // Arrange
        const int userId = 123;
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockContext = GetInMemoryDbContext();

        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext))).Returns(mockContext);
        mockServiceProvider.Setup(x => x.GetService(typeof(IFileLoggingService))).Returns(_mockLogger.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        // Act - Should not throw
        await _service.FetchActivitiesForUserAsync(userId);
        
        // Assert - Should log warning about missing token
        _mockLogger.Verify(
            x => x.LogWarningAsync(It.Is<string>(s => s.Contains("No valid OAuth token found for user")), null),
            Times.Once);
    }

    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}