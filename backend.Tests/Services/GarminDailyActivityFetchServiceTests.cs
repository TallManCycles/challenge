using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using backend.Models;
using backend.Services;

namespace backend.Tests.Services;

public class GarminDailyActivityFetchServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<GarminDailyActivityFetchService>> _mockLogger;
    private readonly Mock<IGarminActivityProcessingService> _mockActivityProcessingService;
    private readonly Mock<IOptions<GarminOAuthConfig>> _mockConfig;
    private readonly GarminDailyActivityFetchService _service;

    public GarminDailyActivityFetchServiceTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<GarminDailyActivityFetchService>>();
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

    [Fact]
    public void Service_Should_Be_Created_Successfully()
    {
        // Test that the service can be instantiated without errors
        Assert.NotNull(_service);
    }

    [Fact]
    public async Task FetchActivitiesForAllUsersAsync_Should_Complete_Without_Exception()
    {
        // Arrange
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockContext = TestHelper.GetInMemoryDbContext();

        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetRequiredService<Data.ApplicationDbContext>()).Returns(mockContext);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        // Act & Assert - Should not throw
        await _service.FetchActivitiesForAllUsersAsync();
        
        // Verify that logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting daily activity fetch for all users")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task FetchActivitiesForUserAsync_Should_Handle_Missing_Token_Gracefully()
    {
        // Arrange
        const int userId = 123;
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockContext = TestHelper.GetInMemoryDbContext();

        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetRequiredService<Data.ApplicationDbContext>()).Returns(mockContext);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        // Act - Should not throw
        await _service.FetchActivitiesForUserAsync(userId);
        
        // Assert - Should log warning about missing token
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No valid OAuth token found for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}