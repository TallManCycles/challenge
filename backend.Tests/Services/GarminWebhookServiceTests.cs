using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text.Json;
using backend.Data;
using backend.Models;
using backend.Services;
using backend.Tests.Helpers;

namespace backend.Tests.Services;

[TestFixture]
public class GarminWebhookServiceTests
{
    private ApplicationDbContext _context;
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private Mock<IFileLoggingService> _mockLogger;
    private Mock<IGarminActivityProcessingService> _mockActivityProcessingService;
    private Mock<IServiceScopeFactory> _mockScopeFactory;
    private Mock<IServiceScope> _mockServiceScope;
    private Mock<IServiceProvider> _mockServiceProvider;
    private GarminWebhookService _service;

    private const string TestGarminPayload = """
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
                    },
                    {
                        "heartRate": 58,
                        "elevationInMeters": 3.0,
                        "startTimeInSeconds": 1754250825,
                        "airTemperatureCelcius": 27.0,
                        "totalDistanceInMeters": 0.0,
                        "clockDurationInSeconds": 2,
                        "timerDurationInSeconds": 2,
                        "movingDurationInSeconds": 0
                    },
                    {
                        "heartRate": 61,
                        "elevationInMeters": 3.0,
                        "startTimeInSeconds": 1754250826,
                        "airTemperatureCelcius": 27.0,
                        "totalDistanceInMeters": 0.0,
                        "clockDurationInSeconds": 3,
                        "timerDurationInSeconds": 3,
                        "movingDurationInSeconds": 0
                    },
                    {
                        "heartRate": 59,
                        "elevationInMeters": 1.600000023841858,
                        "startTimeInSeconds": 1754250828,
                        "airTemperatureCelcius": 27.0,
                        "totalDistanceInMeters": 0.0,
                        "clockDurationInSeconds": 5,
                        "timerDurationInSeconds": 5,
                        "movingDurationInSeconds": 0
                    },
                    {
                        "heartRate": 59,
                        "elevationInMeters": -0.20000000298023224,
                        "startTimeInSeconds": 1754250829,
                        "airTemperatureCelcius": 27.0,
                        "totalDistanceInMeters": 0.0,
                        "clockDurationInSeconds": 6,
                        "timerDurationInSeconds": 6,
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

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<IFileLoggingService>();
        _mockActivityProcessingService = new Mock<IGarminActivityProcessingService>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        // Setup service scope factory
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(ApplicationDbContext))).Returns(_context);

        _service = new GarminWebhookService(
            _mockHttpClientFactory.Object,
            _mockLogger.Object,
            _mockActivityProcessingService.Object,
            _mockScopeFactory.Object);

        // Create test user with OAuth token
        CreateTestUserWithOAuthToken();
    }

    private void CreateTestUserWithOAuthToken()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash"
        };

        var oauthToken = new GarminOAuthToken
        {
            UserId = user.Id,
            AccessToken = "476ab55d-6d42-4077-b146-0f27bab80a1c",
            AccessTokenSecret = "secret",
            RequestTokenSecret = "request-secret",
            RequestToken = "request-token",
            State = "test-state",
            IsAuthorized = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        _context.Users.Add(user);
        _context.Set<GarminOAuthToken>().Add(oauthToken);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestFixture]
    public class ProcessPingNotificationAsyncTests : GarminWebhookServiceTests
    {
        [Test]
        public async Task ProcessPingNotificationAsync_ValidPayload_StoresWebhookPayload()
        {
            // Arrange
            var webhookType = "activity";
            var payload = """{"ping": "test"}""";

            // Act
            var result = await _service.ProcessPingNotificationAsync(webhookType, payload);

            // Assert
            Assert.IsTrue(result);
            
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.AreEqual(GarminWebhookType.Activities, storedPayload!.WebhookType);
            Assert.AreEqual(payload, storedPayload.RawPayload);
            Assert.IsNull(storedPayload.ProcessedAt);
            Assert.IsFalse(storedPayload.IsProcessed);
        }

        [Test]
        public async Task ProcessPingNotificationAsync_InvalidWebhookType_StoresAsUnknown()
        {
            // Arrange
            var webhookType = "invalid-type";
            var payload = """{"ping": "test"}""";

            // Act
            var result = await _service.ProcessPingNotificationAsync(webhookType, payload);

            // Assert
            Assert.IsTrue(result);
            
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.AreEqual(GarminWebhookType.Activities, storedPayload!.WebhookType); // Default to Activities for unknown types
        }

        [Test]
        public async Task ProcessPingNotificationAsync_EmptyPayload_StoresSuccessfully()
        {
            // Arrange
            var webhookType = "activity";
            var payload = "";

            // Act
            var result = await _service.ProcessPingNotificationAsync(webhookType, payload);

            // Assert
            Assert.IsTrue(result);
            
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.AreEqual("", storedPayload!.RawPayload);
        }
    }

    [TestFixture]
    public class ProcessPushNotificationAsyncTests : GarminWebhookServiceTests
    {
        [Test]
        public async Task ProcessPushNotificationAsync_ValidGarminPayload_ParsesAndCreatesActivity()
        {
            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", TestGarminPayload);

            // Assert
            Assert.IsTrue(result);
            
            // Verify webhook payload was stored
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.AreEqual(GarminWebhookType.Activities, storedPayload!.WebhookType);
            Assert.IsTrue(storedPayload.IsProcessed);
            Assert.IsNotNull(storedPayload.ProcessedAt);

            // Verify activity was created
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNotNull(activity);
            Assert.AreEqual(1, activity!.UserId); // Should match our test user
            Assert.AreEqual("19940666749-detail", activity.SummaryId);
            Assert.AreEqual("19940666769", activity.ActivityId);
            Assert.AreEqual(GarminActivityType.CYCLING, activity.ActivityType);
            Assert.AreEqual(6, activity.DurationInSeconds);
            Assert.AreEqual("fenix 6X Pro", activity.DeviceName);
            Assert.IsFalse(activity.IsWebUpload);
            Assert.IsFalse(activity.IsManual);
            Assert.AreEqual(36000, activity.StartTimeOffsetInSeconds);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_GarminPayload_CalculatesElevationFromSamples()
        {
            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", TestGarminPayload);

            // Assert
            Assert.IsTrue(result);
            
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNotNull(activity);
            
            // Expected elevation calculation from samples:
            // Sample 1: 3.0m
            // Sample 2: 3.0m (no change)
            // Sample 3: 3.0m (no change)
            // Sample 4: 1.6m (loss of 1.4m)
            // Sample 5: -0.2m (loss of 1.8m)
            // Total elevation loss should be ~3.2m
            Assert.IsNotNull(activity!.TotalElevationLossInMeters);
            Assert.Greater(activity.TotalElevationLossInMeters!.Value, 3.0);
            Assert.Less(activity.TotalElevationLossInMeters.Value, 4.0);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_GarminPayload_CalculatesMaxDistanceFromSamples()
        {
            // Arrange - Modify payload to include distance data
            var payloadWithDistance = TestGarminPayload.Replace(
                "\"totalDistanceInMeters\": 0.0",
                "\"totalDistanceInMeters\": 100.5");

            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", payloadWithDistance);

            // Assert
            Assert.IsTrue(result);
            
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNotNull(activity);
            Assert.IsNotNull(activity!.DistanceInMeters);
            Assert.AreEqual(100.5, activity.DistanceInMeters!.Value, 0.1);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_GarminPayload_ConvertsUnixTimestamp()
        {
            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", TestGarminPayload);

            // Assert
            Assert.IsTrue(result);
            
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNotNull(activity);
            
            // Unix timestamp 1754250823 should convert to a valid DateTime
            var expectedDateTime = DateTimeOffset.FromUnixTimeSeconds(1754250823).DateTime;
            Assert.AreEqual(expectedDateTime, activity!.StartTime);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_UserNotFound_LogsWarningAndStoresPayload()
        {
            // Arrange - Create payload with non-existent user token
            var payloadWithInvalidUser = TestGarminPayload.Replace(
                "476ab55d-6d42-4077-b146-0f27bab80a1c",
                "non-existent-token");

            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", payloadWithInvalidUser);

            // Assert
            Assert.IsTrue(result); // Should still return true (payload processed)
            
            // Payload should be stored but not processed
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.IsFalse(storedPayload!.IsProcessed);
            
            // No activity should be created
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNull(activity);
            
            // Should log warning
            _mockLogger.Verify(
                x => x.LogWarningAsync(It.Is<string>(s => s.Contains("Could not extract userId")), null),
                Times.Once);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_InvalidJsonPayload_LogsErrorAndStoresPayload()
        {
            // Arrange
            var invalidPayload = """{"invalid": json structure""";

            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", invalidPayload);

            // Assert
            Assert.IsTrue(result); // Should still return true (payload stored)
            
            // Payload should be stored but not processed
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.IsFalse(storedPayload!.IsProcessed);
            Assert.IsNotNull(storedPayload.ProcessingError);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_DuplicateActivity_SkipsCreation()
        {
            // Arrange - Create existing activity
            var existingActivity = new GarminActivity
            {
                UserId = 1,
                SummaryId = "19940666749-detail",
                ActivityId = "19940666769",
                ActivityType = GarminActivityType.CYCLING,
                StartTime = DateTime.UtcNow,
                DurationInSeconds = 3600,
                ReceivedAt = DateTime.UtcNow,
                ResponseData = "{}"
            };
            _context.GarminActivities.Add(existingActivity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", TestGarminPayload);

            // Assert
            Assert.IsTrue(result);
            
            // Should still only have one activity
            var activitiesCount = await _context.GarminActivities.CountAsync();
            Assert.AreEqual(1, activitiesCount);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_ActivityWithCalories_StoresCalorieData()
        {
            // Arrange - Add calories to the payload
            var payloadWithCalories = TestGarminPayload.Replace(
                "\"averageHeartRateInBeatsPerMinute\": 60",
                "\"averageHeartRateInBeatsPerMinute\": 60,\n                \"activeKilocalories\": 250");

            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", payloadWithCalories);

            // Assert
            Assert.IsTrue(result);
            
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNotNull(activity);
            Assert.AreEqual(250, activity!.ActiveKilocalories);
        }

        [Test]
        public async Task ProcessPushNotificationAsync_MultipleActivitiesInPayload_ProcessesAll()
        {
            // Arrange - Create payload with multiple activities
            var multipleActivitiesPayload = """
            {
                "activityDetails": [
                    {
                        "summary": {
                            "activityId": 19940666749,
                            "activityType": "CYCLING",
                            "durationInSeconds": 6,
                            "startTimeInSeconds": 1754250823
                        },
                        "summaryId": "19940666749-detail",
                        "activityId": 19940666769,
                        "userAccessToken": "476ab55d-6d42-4077-b146-0f27bab80a1c"
                    },
                    {
                        "summary": {
                            "activityId": 19940666750,
                            "activityType": "RUNNING",
                            "durationInSeconds": 1800,
                            "startTimeInSeconds": 1754250900
                        },
                        "summaryId": "19940666750-detail",
                        "activityId": 19940666770,
                        "userAccessToken": "476ab55d-6d42-4077-b146-0f27bab80a1c"
                    }
                ]
            }
            """;

            // Act
            var result = await _service.ProcessPushNotificationAsync("activity", multipleActivitiesPayload);

            // Assert
            Assert.IsTrue(result);
            
            var activitiesCount = await _context.GarminActivities.CountAsync();
            Assert.AreEqual(2, activitiesCount);
            
            var activities = await _context.GarminActivities.ToListAsync();
            Assert.IsTrue(activities.Any(a => a.ActivityType == GarminActivityType.CYCLING));
            Assert.IsTrue(activities.Any(a => a.ActivityType == GarminActivityType.RUNNING));
        }
    }

    [TestFixture]
    public class ProcessStoredPayloadsAsyncTests : GarminWebhookServiceTests
    {
        [Test]
        public async Task ProcessStoredPayloadsAsync_UnprocessedPayloads_ProcessesThem()
        {
            // Arrange - Create unprocessed payloads
            var unprocessedPayload = new GarminWebhookPayload
            {
                WebhookType = GarminWebhookType.Activities,
                RawPayload = TestGarminPayload,
                ReceivedAt = DateTime.UtcNow.AddMinutes(-10),
                IsProcessed = false
            };
            _context.GarminWebhookPayloads.Add(unprocessedPayload);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcessStoredPayloadsAsync();

            // Assert
            var payload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(payload);
            Assert.IsTrue(payload!.IsProcessed);
            Assert.IsNotNull(payload.ProcessedAt);
            
            // Activity should be created
            var activity = await _context.GarminActivities.FirstOrDefaultAsync();
            Assert.IsNotNull(activity);
        }

        [Test]
        public async Task ProcessStoredPayloadsAsync_AlreadyProcessedPayloads_SkipsThem()
        {
            // Arrange - Create already processed payload
            var processedPayload = new GarminWebhookPayload
            {
                WebhookType = GarminWebhookType.Activities,
                RawPayload = TestGarminPayload,
                ReceivedAt = DateTime.UtcNow.AddMinutes(-10),
                IsProcessed = true,
                ProcessedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            _context.GarminWebhookPayloads.Add(processedPayload);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcessStoredPayloadsAsync();

            // Assert - ProcessedAt shouldn't change
            var payload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(payload);
            Assert.AreEqual(processedPayload.ProcessedAt, payload!.ProcessedAt);
        }

        [Test]
        public async Task ProcessStoredPayloadsAsync_FailedPayload_MarksWithError()
        {
            // Arrange - Create payload with invalid JSON
            var failedPayload = new GarminWebhookPayload
            {
                WebhookType = GarminWebhookType.Activities,
                RawPayload = """{"invalid": json""",
                ReceivedAt = DateTime.UtcNow.AddMinutes(-10),
                IsProcessed = false
            };
            _context.GarminWebhookPayloads.Add(failedPayload);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcessStoredPayloadsAsync();

            // Assert
            var payload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(payload);
            Assert.IsFalse(payload!.IsProcessed);  // Should remain unprocessed
            Assert.IsNotNull(payload.ProcessingError);
            Assert.IsTrue(payload.ProcessingError!.Contains("Error processing"));
        }
    }

    [TestFixture]
    public class WebhookTypeParsingTests : GarminWebhookServiceTests
    {
        [Test, TestCase("activities", GarminWebhookType.Activities)]
        [TestCase("ACTIVITIES", GarminWebhookType.Activities)]
        [TestCase("Activities", GarminWebhookType.Activities)]
        [TestCase("activityDetails", GarminWebhookType.ActivityDetails)]
        [TestCase("unknown-type", GarminWebhookType.Activities)] // Default to Activities for unknown
        [TestCase("", GarminWebhookType.Activities)]
        public async Task ProcessPingNotificationAsync_WebhookTypeParsing_CorrectlyParsesTypes(
            string webhookType, GarminWebhookType expectedType)
        {
            // Arrange
            var payload = """{"test": "payload"}""";

            // Act
            await _service.ProcessPingNotificationAsync(webhookType, payload);

            // Assert
            var storedPayload = await _context.GarminWebhookPayloads.FirstOrDefaultAsync();
            Assert.IsNotNull(storedPayload);
            Assert.AreEqual(expectedType, storedPayload!.WebhookType);
        }
    }
}