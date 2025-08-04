using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using backend.Controllers;
using backend.Models;
using backend.Services;

namespace backend.Tests.Controllers;

[TestFixture]
public class GarminActivitiesControllerTests
{
    private Mock<IGarminActivityProcessingService> _mockActivityService;
    private Mock<IFileLoggingService> _mockLogger;
    private GarminActivitiesController _controller;

    [SetUp]
    public void Setup()
    {
        _mockActivityService = new Mock<IGarminActivityProcessingService>();
        _mockLogger = new Mock<IFileLoggingService>();
        _controller = new GarminActivitiesController(_mockActivityService.Object, _mockLogger.Object);
        
        // Setup user context
        SetupUserContext(123);
    }

    private void SetupUserContext(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [TestFixture]
    public class GetUserActivitiesTests : GarminActivitiesControllerTests
    {
        [Test]
        public async Task GetUserActivities_ValidRequest_ReturnsOkWithActivities()
        {
            // Arrange
            var activities = new List<GarminActivity>
            {
                new GarminActivity
                {
                    Id = 1,
                    SummaryId = "test-summary-1",
                    ActivityId = "activity-1",
                    ActivityType = GarminActivityType.CYCLING,
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    DurationInSeconds = 3600,
                    DistanceInMeters = 25000,
                    TotalElevationGainInMeters = 500,
                    ActiveKilocalories = 800,
                    DeviceName = "Edge 830",
                    IsManual = false,
                    IsWebUpload = false,
                    ReceivedAt = DateTime.UtcNow
                },
                new GarminActivity
                {
                    Id = 2,
                    SummaryId = "test-summary-2",
                    ActivityId = "activity-2",
                    ActivityType = GarminActivityType.RUNNING,
                    StartTime = DateTime.UtcNow.AddHours(-1),
                    DurationInSeconds = 1800,
                    DistanceInMeters = 5000,
                    ActiveKilocalories = 300,
                    DeviceName = "Fenix 6",
                    IsManual = false,
                    IsWebUpload = false,
                    ReceivedAt = DateTime.UtcNow
                }
            };

            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(123, 1, 20))
                .ReturnsAsync(activities);
            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(123, 2, 1))
                .ReturnsAsync(new List<GarminActivity>());

            // Act
            var result = await _controller.GetUserActivities(1, 20);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            
            // Use reflection to check the anonymous object
            var activitiesProperty = response!.GetType().GetProperty("activities");
            var hasMoreProperty = response.GetType().GetProperty("hasMore");
            var pageProperty = response.GetType().GetProperty("page");
            
            Assert.IsNotNull(activitiesProperty);
            Assert.IsNotNull(hasMoreProperty);
            Assert.IsNotNull(pageProperty);
            
            var responseActivities = activitiesProperty!.GetValue(response) as IEnumerable<object>;
            Assert.IsNotNull(responseActivities);
            Assert.AreEqual(2, responseActivities!.Count());
            
            Assert.AreEqual(false, hasMoreProperty!.GetValue(response));
            Assert.AreEqual(1, pageProperty!.GetValue(response));
        }

        [Test]
        public async Task GetUserActivities_InvalidUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext(0); // Invalid user ID
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.GetUserActivities();

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task GetUserActivities_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserActivities();

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(500, objectResult!.StatusCode);
        }

        [Test]
        public async Task GetUserActivities_LimitPageSize_EnforcesMaximum()
        {
            // Arrange
            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(123, 1, 50))
                .ReturnsAsync(new List<GarminActivity>());
            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(123, 2, 1))
                .ReturnsAsync(new List<GarminActivity>());

            // Act
            var result = await _controller.GetUserActivities(1, 100); // Request 100, should be limited to 50

            // Assert
            _mockActivityService.Verify(s => s.GetUserActivitiesAsync(123, 1, 50), Times.Once);
        }

        [Test]
        public async Task GetUserActivities_ActivityTypeAsString_ReturnsStringValue()
        {
            // Arrange
            var activities = new List<GarminActivity>
            {
                new GarminActivity
                {
                    Id = 1,
                    ActivityType = GarminActivityType.CYCLING,
                    StartTime = DateTime.UtcNow,
                    DurationInSeconds = 3600,
                    ReceivedAt = DateTime.UtcNow
                }
            };

            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(123, 1, 20))
                .ReturnsAsync(activities);
            _mockActivityService.Setup(s => s.GetUserActivitiesAsync(123, 2, 1))
                .ReturnsAsync(new List<GarminActivity>());

            // Act
            var result = await _controller.GetUserActivities();

            // Assert
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            var activitiesProperty = response!.GetType().GetProperty("activities");
            var responseActivities = activitiesProperty!.GetValue(response) as IEnumerable<object>;
            var firstActivity = responseActivities!.First();
            var activityTypeProperty = firstActivity.GetType().GetProperty("ActivityType");
            
            Assert.AreEqual("CYCLING", activityTypeProperty!.GetValue(firstActivity));
        }
    }

    [TestFixture]
    public class GetCyclingActivitiesTests : GarminActivitiesControllerTests
    {
        [Test]
        public async Task GetCyclingActivities_ValidRequest_ReturnsOkWithActivities()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-30);
            var toDate = DateTime.UtcNow;
            var activities = new List<GarminActivity>
            {
                new GarminActivity
                {
                    Id = 1,
                    ActivityType = GarminActivityType.CYCLING,
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    DurationInSeconds = 3600,
                    ReceivedAt = DateTime.UtcNow
                }
            };

            _mockActivityService.Setup(s => s.GetCyclingActivitiesAsync(123, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(activities);

            // Act
            var result = await _controller.GetCyclingActivities(fromDate, toDate);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            
            var activitiesProperty = response!.GetType().GetProperty("activities");
            var totalActivitiesProperty = response.GetType().GetProperty("totalActivities");
            
            Assert.IsNotNull(activitiesProperty);
            Assert.IsNotNull(totalActivitiesProperty);
            Assert.AreEqual(1, totalActivitiesProperty!.GetValue(response));
        }

        [Test]
        public async Task GetCyclingActivities_NoDatesProvided_UsesDefaults()
        {
            // Arrange
            _mockActivityService.Setup(s => s.GetCyclingActivitiesAsync(123, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<GarminActivity>());

            // Act
            var result = await _controller.GetCyclingActivities();

            // Assert
            _mockActivityService.Verify(s => s.GetCyclingActivitiesAsync(
                123, 
                It.Is<DateTime>(d => d <= DateTime.UtcNow.AddMonths(-3).AddDays(1) && d >= DateTime.UtcNow.AddMonths(-3).AddDays(-1)),
                It.Is<DateTime>(d => d <= DateTime.UtcNow.AddDays(1) && d >= DateTime.UtcNow.AddDays(-1))
            ), Times.Once);
        }

        [Test]
        public async Task GetCyclingActivities_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _mockActivityService.Setup(s => s.GetCyclingActivitiesAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCyclingActivities();

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(500, objectResult!.StatusCode);
        }
    }

    [TestFixture]
    public class GetActivityDetailsTests : GarminActivitiesControllerTests
    {
        [Test]
        public async Task GetActivityDetails_ValidRequest_ReturnsOkWithActivity()
        {
            // Arrange
            var activity = new GarminActivity
            {
                Id = 1,
                SummaryId = "test-summary",
                ActivityId = "activity-id",
                ActivityType = GarminActivityType.CYCLING,
                StartTime = DateTime.UtcNow.AddHours(-2),
                StartTimeOffsetInSeconds = 3600,
                DurationInSeconds = 3600,
                DistanceInMeters = 25000,
                TotalElevationGainInMeters = 500,
                ActiveKilocalories = 800,
                DeviceName = "Edge 830",
                IsManual = false,
                IsWebUpload = false,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                IsProcessed = true
            };

            _mockActivityService.Setup(s => s.GetActivityDetailsAsync(1, 123))
                .ReturnsAsync(activity);

            // Act
            var result = await _controller.GetActivityDetails(1);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            
            var idProperty = response!.GetType().GetProperty("Id");
            var activityTypeProperty = response.GetType().GetProperty("ActivityType");
            var isProcessedProperty = response.GetType().GetProperty("IsProcessed");
            
            Assert.AreEqual(1, idProperty!.GetValue(response));
            Assert.AreEqual("CYCLING", activityTypeProperty!.GetValue(response));
            Assert.AreEqual(true, isProcessedProperty!.GetValue(response));
        }

        [Test]
        public async Task GetActivityDetails_ActivityNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockActivityService.Setup(s => s.GetActivityDetailsAsync(999, 123))
                .ReturnsAsync((GarminActivity?)null);

            // Act
            var result = await _controller.GetActivityDetails(999);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task GetActivityDetails_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _mockActivityService.Setup(s => s.GetActivityDetailsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetActivityDetails(1);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(500, objectResult!.StatusCode);
        }

        [Test]
        public async Task GetActivityDetails_DateTimeSpecifiedAsUtc_ReturnsUtcDates()
        {
            // Arrange
            var utcStartTime = DateTime.UtcNow.AddHours(-2);
            var utcReceivedAt = DateTime.UtcNow.AddMinutes(-30);
            var utcProcessedAt = DateTime.UtcNow.AddMinutes(-20);

            var activity = new GarminActivity
            {
                Id = 1,
                ActivityType = GarminActivityType.CYCLING,
                StartTime = utcStartTime,
                DurationInSeconds = 3600,
                ReceivedAt = utcReceivedAt,
                ProcessedAt = utcProcessedAt,
                IsProcessed = true
            };

            _mockActivityService.Setup(s => s.GetActivityDetailsAsync(1, 123))
                .ReturnsAsync(activity);

            // Act
            var result = await _controller.GetActivityDetails(1);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            
            var startTimeProperty = response!.GetType().GetProperty("StartTime");
            var receivedAtProperty = response.GetType().GetProperty("ReceivedAt");
            var processedAtProperty = response.GetType().GetProperty("ProcessedAt");
            
            var startTime = (DateTime)startTimeProperty!.GetValue(response)!;
            var receivedAt = (DateTime)receivedAtProperty!.GetValue(response)!;
            var processedAt = (DateTime?)processedAtProperty!.GetValue(response);
            
            Assert.AreEqual(DateTimeKind.Utc, startTime.Kind);
            Assert.AreEqual(DateTimeKind.Utc, receivedAt.Kind);
            Assert.AreEqual(DateTimeKind.Utc, processedAt!.Value.Kind);
        }
    }
}