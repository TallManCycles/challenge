using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Text;
using backend.Controllers;
using backend.Services;
using Microsoft.Extensions.Logging;

namespace backend.Tests.Controllers;

[TestFixture]
public class GarminWebhookControllerTests
{
    private Mock<IGarminWebhookService> _mockWebhookService;
    private Mock<IFileLoggingService> _mockLogger;
    private GarminWebhookController _controller;

    [SetUp]
    public void Setup()
    {
        _mockWebhookService = new Mock<IGarminWebhookService>();
        _mockLogger = new Mock<IFileLoggingService>();
        _controller = new GarminWebhookController(_mockWebhookService.Object, _mockLogger.Object);
    }

    private void SetupHttpContext(string requestBody)
    {
        var httpContext = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentLength = bodyBytes.Length;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [TestFixture]
    public class HandlePingWebhookTests : GarminWebhookControllerTests
    {
        [Test]
        public async Task HandlePingWebhook_ValidRequest_ReturnsOk()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "ping payload"}""";
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPingNotificationAsync(webhookType, payload))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.HandlePingWebhook(webhookType);

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            _mockWebhookService.Verify(s => s.ProcessPingNotificationAsync(webhookType, payload), Times.Once);
        }

        [Test]
        public async Task HandlePingWebhook_ServiceThrows_StillReturnsOk()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "ping payload"}""";
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPingNotificationAsync(webhookType, payload))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.HandlePingWebhook(webhookType);

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            // Verify error was logged
            _mockLogger.Verify(
                x => x.LogErrorAsync(
                    "Error processing ping webhook asynchronously",
                    It.IsAny<Exception>(),
                    "GarminWebhookController"),
                Times.Once);
        }

        [Test]
        public async Task HandlePingWebhook_EmptyPayload_ProcessesSuccessfully()
        {
            // Arrange
            var webhookType = "activity";
            var payload = "";
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPingNotificationAsync(webhookType, payload))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.HandlePingWebhook(webhookType);

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            _mockWebhookService.Verify(s => s.ProcessPingNotificationAsync(webhookType, payload), Times.Once);
        }

        [Test]
        public async Task HandlePingWebhook_LogsInformation()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "ping payload"}""";
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPingNotificationAsync(webhookType, payload))
                .ReturnsAsync(true);

            // Act
            await _controller.HandlePingWebhook(webhookType);

            // Assert
            _mockLogger.Verify(
                x => x.LogInfoAsync(
                    It.Is<string>((v, t) => v.ToString()!.Contains("Received ping webhook for type")),
                    It.IsAny<string>()),
                Times.Once);
        }
    }

    [TestFixture]
    public class HandlePushWebhookTests : GarminWebhookControllerTests
    {
        [Test]
        public async Task HandlePushWebhook_ValidRequest_ReturnsOk()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """
            {
                "activityDetails": [
                    {
                        "summary": {
                            "activityId": 19940666749,
                            "activityType": "CYCLING"
                        }
                    }
                ]
            }
            """;
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPushNotificationAsync(webhookType, payload))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.HandlePushWebhook(webhookType);

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            _mockWebhookService.Verify(s => s.ProcessPushNotificationAsync(webhookType, payload), Times.Once);
        }

        [Test]
        public async Task HandlePushWebhook_ServiceThrows_StillReturnsOk()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "push payload"}""";
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPushNotificationAsync(webhookType, payload))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.HandlePushWebhook(webhookType);

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            // Verify error was logged
            _mockLogger.Verify(
                x => x.LogErrorAsync(
                    It.Is<string>((v, t) => v.ToString()!.Contains("Error processing push webhook asynchronously")),
                    It.IsAny<Exception>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task HandlePushWebhook_LogsInformation()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "push payload"}""";
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPushNotificationAsync(webhookType, payload))
                .ReturnsAsync(true);

            // Act
            await _controller.HandlePushWebhook(webhookType);

            // Assert
            _mockLogger.Verify(
                x => x.LogInfoAsync(
                    It.Is<string>((v, t) => v.ToString()!.Contains("Received push webhook for type")),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task HandlePushWebhook_LargePayload_ProcessesSuccessfully()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """
            {
                "activityDetails": [
                    {
                        "laps": [
                            {
                                "startTimeInSeconds": 1754250823
                            }
                        ],
                        "userId": "2b04660ac3fab48649d1cdda8888fd95",
                        "samples": [
                            {
                                "heartRate": 61,
                                "elevationInMeters": 3.0,
                                "startTimeInSeconds": 1754250823,
                                "airTemperatureCelcius": 27.0,
                                "totalDistanceInMeters": 0.0,
                                "clockDurationInSeconds": 0,
                                "timerDurationInSeconds": 0,
                                "movingDurationInSeconds": 0
                            }
                        ],
                        "summary": {
                            "activityId": 19940666749,
                            "deviceName": "fenix 6X Pro",
                            "isWebUpload": false,
                            "activityName": "Cycling",
                            "activityType": "CYCLING",
                            "durationInSeconds": 6,
                            "startTimeInSeconds": 1754250823,
                            "startTimeOffsetInSeconds": 36000,
                            "maxHeartRateInBeatsPerMinute": 63,
                            "averageHeartRateInBeatsPerMinute": 60
                        },
                        "summaryId": "19940666749-detail",
                        "activityId": 19940666769,
                        "userAccessToken": "476ab55d-6d42-4077-b146-0f27bab80a1c"
                    }
                ]
            }
            """;
            
            SetupHttpContext(payload);
            
            _mockWebhookService.Setup(s => s.ProcessPushNotificationAsync(webhookType, payload))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.HandlePushWebhook(webhookType);

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            _mockWebhookService.Verify(s => s.ProcessPushNotificationAsync(webhookType, payload), Times.Once);
        }
    }

    [TestFixture]
    public class ProcessFailedPayloadsTests : GarminWebhookControllerTests
    {
        [Test]
        public async Task ProcessFailedPayloads_ValidRequest_ReturnsOkWithMessage()
        {
            // Arrange
            _mockWebhookService.Setup(s => s.ProcessStoredPayloadsAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ProcessFailedPayloads();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            
            var messageProperty = response!.GetType().GetProperty("message");
            Assert.AreEqual("Failed payloads processing initiated", messageProperty!.GetValue(response));
            
            _mockWebhookService.Verify(s => s.ProcessStoredPayloadsAsync(), Times.Once);
        }

        [Test]
        public async Task ProcessFailedPayloads_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _mockWebhookService.Setup(s => s.ProcessStoredPayloadsAsync())
                .ThrowsAsync(new Exception("Processing error"));

            // Act
            var result = await _controller.ProcessFailedPayloads();

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(500, objectResult!.StatusCode);
            
            var response = objectResult.Value;
            var errorProperty = response!.GetType().GetProperty("error");
            Assert.AreEqual("Failed to process failed payloads", errorProperty!.GetValue(response));
        }

        [Test]
        public async Task ProcessFailedPayloads_ServiceThrows_LogsError()
        {
            // Arrange
            var exception = new Exception("Processing error");
            _mockWebhookService.Setup(s => s.ProcessStoredPayloadsAsync())
                .ThrowsAsync(exception);

            // Act
            await _controller.ProcessFailedPayloads();

            // Assert
            _mockLogger.Verify(
                x => x.LogErrorAsync(
                    It.Is<string>((v, t) => v.ToString()!.Contains("Error processing failed payloads")),
                    exception,
                    It.IsAny<string>())
                ,Times.Once);
        }
    }

    [TestFixture]
    public class AsyncProcessingTests : GarminWebhookControllerTests
    {
        [Test]
        public async Task HandlePingWebhook_ProcessesAsynchronously_ReturnsImmediately()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "ping payload"}""";
            var processingDelay = TimeSpan.FromMilliseconds(100);
            
            SetupHttpContext(payload);
            
            // Setup service to take some time
            _mockWebhookService.Setup(s => s.ProcessPingNotificationAsync(webhookType, payload))
                .Returns(async () =>
                {
                    await Task.Delay(processingDelay);
                    return true;
                });

            var startTime = DateTime.UtcNow;

            // Act
            var result = await _controller.HandlePingWebhook(webhookType);
            var responseTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            // Response should return quickly, not wait for processing
            Assert.Less(responseTime, processingDelay);
        }

        [Test]
        public async Task HandlePushWebhook_ProcessesAsynchronously_ReturnsImmediately()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"test": "push payload"}""";
            var processingDelay = TimeSpan.FromMilliseconds(100);
            
            SetupHttpContext(payload);
            
            // Setup service to take some time
            _mockWebhookService.Setup(s => s.ProcessPushNotificationAsync(webhookType, payload))
                .Returns(async () =>
                {
                    await Task.Delay(processingDelay);
                    return true;
                });

            var startTime = DateTime.UtcNow;

            // Act
            var result = await _controller.HandlePushWebhook(webhookType);
            var responseTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.IsInstanceOf<OkResult>(result);
            // Response should return quickly, not wait for processing
            Assert.Less(responseTime, processingDelay);
        }
    }
}