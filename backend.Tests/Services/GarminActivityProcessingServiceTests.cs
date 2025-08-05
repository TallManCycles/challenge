using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using backend.Data;
using backend.Models;
using backend.Services;
using backend.Tests.Helpers;

namespace backend.Tests.Services;

[TestFixture]
public class GarminActivityProcessingServiceTests
{
    private ApplicationDbContext _context;
    private Mock<ILogger<GarminActivityProcessingService>> _mockLogger;
    private Mock<IChallengeNotificationService> _mockNotificationService;
    private GarminActivityProcessingService _service;

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<GarminActivityProcessingService>>();
        _mockNotificationService = new Mock<IChallengeNotificationService>();
        _service = new GarminActivityProcessingService(_context, _mockLogger.Object, _mockNotificationService.Object);
        
        CreateTestData();
    }

    private void CreateTestData()
    {
        // Create test users
        var user1 = new User { Id = 1, Email = "user1@test.com", Username = "user1", PasswordHash = "hash1" };
        var user2 = new User { Id = 2, Email = "user2@test.com", Username = "user2", PasswordHash = "hash2" };
        
        _context.Users.AddRange(user1, user2);

        // Create test challenges
        var challenge1 = new Challenge
        {
            Id = 1,
            Title = "Distance Challenge",
            Description = "Cycle 100km",
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedById = 1
        };

        var challenge2 = new Challenge
        {
            Id = 2,
            Title = "Elevation Challenge", 
            Description = "Climb 1000m",
            ChallengeType = ChallengeType.Elevation,
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            CreatedById = 2
        };

        var inactiveChallenge = new Challenge
        {
            Id = 3,
            Title = "Inactive Challenge",
            Description = "Old challenge",
            ChallengeType = ChallengeType.Time,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(-20),
            IsActive = false,
            CreatedById = 1
        };

        _context.Challenges.AddRange(challenge1, challenge2, inactiveChallenge);

        // Create challenge participants
        var participant1 = new ChallengeParticipant { ChallengeId = 1, UserId = 1, JoinedAt = DateTime.UtcNow.AddDays(-7) };
        var participant2 = new ChallengeParticipant { ChallengeId = 1, UserId = 2, JoinedAt = DateTime.UtcNow.AddDays(-6) };
        var participant3 = new ChallengeParticipant { ChallengeId = 2, UserId = 1, JoinedAt = DateTime.UtcNow.AddDays(-3) };

        _context.ChallengeParticipants.AddRange(participant1, participant2, participant3);

        // Create test activities
        var activity1 = new GarminActivity
        {
            Id = 1,
            UserId = 1,
            SummaryId = "summary1",
            ActivityType = GarminActivityType.CYCLING,
            StartTime = DateTime.UtcNow.AddDays(-2),
            DurationInSeconds = 3600,
            DistanceInMeters = 25000,
            TotalElevationGainInMeters = 500,
            ActiveKilocalories = 800,
            DeviceName = "Edge 830",
            ReceivedAt = DateTime.UtcNow.AddDays(-2),
            ResponseData = "{}"
        };

        var activity2 = new GarminActivity
        {
            Id = 2,
            UserId = 2,
            SummaryId = "summary2",
            ActivityType = GarminActivityType.RUNNING,
            StartTime = DateTime.UtcNow.AddDays(-1),
            DurationInSeconds = 1800,
            DistanceInMeters = 5000,
            TotalElevationGainInMeters = 100,
            ActiveKilocalories = 300,
            DeviceName = "Fenix 6",
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
            ResponseData = "{}"
        };

        var activity3 = new GarminActivity
        {
            Id = 3,
            UserId = 1,
            SummaryId = "summary3",
            ActivityType = GarminActivityType.CYCLING,
            StartTime = DateTime.UtcNow.AddHours(-6),
            DurationInSeconds = 7200,
            DistanceInMeters = 50000,
            TotalElevationGainInMeters = 1200,
            ActiveKilocalories = 1500,
            DeviceName = "Edge 830",
            ReceivedAt = DateTime.UtcNow.AddHours(-6),
            ResponseData = "{}"
        };

        // Activity outside challenge date range
        var oldActivity = new GarminActivity
        {
            Id = 4,
            UserId = 1,
            SummaryId = "summary4",
            ActivityType = GarminActivityType.CYCLING,
            StartTime = DateTime.UtcNow.AddDays(-15),
            DurationInSeconds = 3600,
            DistanceInMeters = 30000,
            TotalElevationGainInMeters = 600,
            ActiveKilocalories = 900,
            DeviceName = "Edge 830",
            ReceivedAt = DateTime.UtcNow.AddDays(-15),
            ResponseData = "{}"
        };

        _context.GarminActivities.AddRange(activity1, activity2, activity3, oldActivity);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestFixture]
    public class ProcessActivityForChallengesAsyncTests : GarminActivityProcessingServiceTests
    {
        [Test]
        public async Task ProcessActivityForChallengesAsync_ValidActivity_UpdatesChallengeParticipants()
        {
            // Act
            await _service.ProcessActivityForChallengesAsync(1); // Cycling activity from User 1

            // Assert
            var participants = await _context.ChallengeParticipants
                .Where(cp => cp.UserId == 1)
                .ToListAsync();
            
            // Should update participant totals for both distance and elevation challenges
            var distanceParticipant = participants.FirstOrDefault(p => p.ChallengeId == 1);
            var elevationParticipant = participants.FirstOrDefault(p => p.ChallengeId == 2);
            
            Assert.IsNotNull(distanceParticipant);
            Assert.IsNotNull(elevationParticipant);
            
            // Verify distance challenge updated (25000m = 25km)
            Assert.AreEqual(25, distanceParticipant!.CurrentTotal);
            
            // Verify elevation challenge updated (500m elevation gain)
            Assert.AreEqual(500, elevationParticipant!.CurrentTotal);
            
            // Verify last activity date updated
            Assert.IsNotNull(distanceParticipant.LastActivityDate);
            Assert.IsNotNull(elevationParticipant.LastActivityDate);
        }

        [Test]
        public async Task ProcessActivityForChallengesAsync_ActivityNotFound_LogsWarning()
        {
            // Act
            await _service.ProcessActivityForChallengesAsync(999); // Non-existent activity

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Activity 999 not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            // No challenge participant totals should be updated
            var participants = await _context.ChallengeParticipants.ToListAsync();
            Assert.IsTrue(participants.All(p => p.CurrentTotal == 0));
        }

        [Test]
        public async Task ProcessActivityForChallengesAsync_ActivityOutsideChallengeTimeRange_DoesNotCreateEntries()
        {
            // Act - Process old activity that's outside challenge date range
            await _service.ProcessActivityForChallengesAsync(4);

            // Assert
            var participants = await _context.ChallengeParticipants.ToListAsync();
            Assert.IsTrue(participants.All(p => p.CurrentTotal == 0));
        }

        [Test]
        public async Task ProcessActivityForChallengesAsync_UserNotParticipatingInChallenges_DoesNotCreateEntries()
        {
            // Arrange - Remove user from challenges
            var userParticipants = await _context.ChallengeParticipants.Where(cp => cp.UserId == 1).ToListAsync();
            _context.ChallengeParticipants.RemoveRange(userParticipants);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcessActivityForChallengesAsync(1);

            // Assert
            var allParticipants = await _context.ChallengeParticipants.ToListAsync();
            Assert.IsTrue(allParticipants.All(p => p.CurrentTotal == 0));
        }

        [Test]
        public async Task ProcessActivityForChallengesAsync_InactiveChallenges_DoesNotCreateEntries()
        {
            // Arrange - Make all challenges inactive
            var challenges = await _context.Challenges.ToListAsync();
            challenges.ForEach(c => c.IsActive = false);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcessActivityForChallengesAsync(1);

            // Assert
            var participants = await _context.ChallengeParticipants.ToListAsync();
            Assert.IsTrue(participants.All(p => p.CurrentTotal == 0));
        }

        [Test]
        public async Task ProcessActivityForChallengesAsync_DuplicateProcessing_DoesNotCreateDuplicates()
        {
            // Act - Process same activity twice
            await _service.ProcessActivityForChallengesAsync(1);
            await _service.ProcessActivityForChallengesAsync(1);

            // Assert - Totals should be doubled if processed twice (no deduplication at this level)
            var participants = await _context.ChallengeParticipants
                .Where(cp => cp.UserId == 1)
                .ToListAsync();
            
            var distanceParticipant = participants.FirstOrDefault(p => p.ChallengeId == 1);
            var elevationParticipant = participants.FirstOrDefault(p => p.ChallengeId == 2);
            
            // Values should be doubled if processed twice
            Assert.AreEqual(50, distanceParticipant!.CurrentTotal); // 25km * 2
            Assert.AreEqual(1000, elevationParticipant!.CurrentTotal); // 500m * 2
        }
    }

    [TestFixture]
    public class GetCyclingActivitiesAsyncTests : GarminActivityProcessingServiceTests
    {
        [Test]
        public async Task GetCyclingActivitiesAsync_ValidDateRange_ReturnsCyclingActivities()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-10);
            var toDate = DateTime.UtcNow;

            // Act
            var result = await _service.GetCyclingActivitiesAsync(1, fromDate, toDate);

            // Assert
            Assert.AreEqual(2, result.Count); // User 1 has 2 cycling activities in range
            Assert.IsTrue(result.All(a => a.ActivityType == GarminActivityType.CYCLING));
            Assert.IsTrue(result.All(a => a.UserId == 1));
            Assert.IsTrue(result.All(a => a.StartTime >= fromDate && a.StartTime <= toDate));
        }

        [Test]
        public async Task GetCyclingActivitiesAsync_OnlyNonCyclingActivities_ReturnsEmpty()
        {
            // Act - Get activities for user 2 who only has running activities
            var result = await _service.GetCyclingActivitiesAsync(2, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetCyclingActivitiesAsync_OutsideDateRange_ReturnsEmpty()
        {
            // Arrange - Date range that excludes all activities
            var fromDate = DateTime.UtcNow.AddDays(-30);
            var toDate = DateTime.UtcNow.AddDays(-20);

            // Act
            var result = await _service.GetCyclingActivitiesAsync(1, fromDate, toDate);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetCyclingActivitiesAsync_OrdersByStartTimeDescending()
        {
            // Act
            var result = await _service.GetCyclingActivitiesAsync(1, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);

            // Assert
            Assert.AreEqual(2, result.Count);
            // More recent activity should be first
            Assert.Greater(result[0].StartTime, result[1].StartTime);
        }

        [Test]
        public async Task GetCyclingActivitiesAsync_IncludesBoundaryDates()
        {
            // Arrange - Set exact boundary dates
            var activity = await _context.GarminActivities.FirstAsync(a => a.Id == 1);
            var fromDate = activity.StartTime.Date;
            var toDate = activity.StartTime.Date.AddDays(1);

            // Act
            var result = await _service.GetCyclingActivitiesAsync(1, fromDate, toDate);

            // Assert
            Assert.Greater(result.Count, 0);
            Assert.IsTrue(result.Any(a => a.Id == 1));
        }
    }

    [TestFixture]
    public class GetUserActivitiesAsyncTests : GarminActivityProcessingServiceTests
    {
        [Test]
        public async Task GetUserActivitiesAsync_DefaultPagination_ReturnsFirstPage()
        {
            // Act
            var result = await _service.GetUserActivitiesAsync(1);

            // Assert
            Assert.AreEqual(3, result.Count); // User 1 has 3 activities total
            Assert.IsTrue(result.All(a => a.UserId == 1));
        }

        [Test]
        public async Task GetUserActivitiesAsync_WithPagination_ReturnsCorrectPage()
        {
            // Act
            var page1 = await _service.GetUserActivitiesAsync(1, 1, 2);
            var page2 = await _service.GetUserActivitiesAsync(1, 2, 2);

            // Assert
            Assert.AreEqual(2, page1.Count);
            Assert.AreEqual(1, page2.Count);
            
            // Should not have duplicate activities
            var allIds = page1.Select(a => a.Id).Concat(page2.Select(a => a.Id)).ToList();
            Assert.AreEqual(3, allIds.Distinct().Count());
        }

        [Test]
        public async Task GetUserActivitiesAsync_OrdersByStartTimeDescending()
        {
            // Act
            var result = await _service.GetUserActivitiesAsync(1);

            // Assert
            Assert.AreEqual(3, result.Count);
            
            // Verify ordering (most recent first)
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.GreaterOrEqual(result[i].StartTime, result[i + 1].StartTime);
            }
        }

        [Test]
        public async Task GetUserActivitiesAsync_UserWithNoActivities_ReturnsEmpty()
        {
            // Arrange - Create user with no activities
            var newUser = new User { Id = 99, Email = "new@test.com", Username = "newuser", PasswordHash = "hash" };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserActivitiesAsync(99);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetUserActivitiesAsync_PageBeyondAvailable_ReturnsEmpty()
        {
            // Act
            var result = await _service.GetUserActivitiesAsync(1, 10, 20); // Way beyond available data

            // Assert
            Assert.AreEqual(0, result.Count);
        }
    }

    [TestFixture]
    public class GetActivityDetailsAsyncTests : GarminActivityProcessingServiceTests
    {
        [Test]
        public async Task GetActivityDetailsAsync_ValidActivityAndUser_ReturnsActivity()
        {
            // Act
            var result = await _service.GetActivityDetailsAsync(1, 1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result!.Id);
            Assert.AreEqual(1, result.UserId);
            Assert.AreEqual("summary1", result.SummaryId);
            Assert.AreEqual(GarminActivityType.CYCLING, result.ActivityType);
        }

        [Test]
        public async Task GetActivityDetailsAsync_ActivityNotFound_ReturnsNull()
        {
            // Act
            var result = await _service.GetActivityDetailsAsync(999, 1);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetActivityDetailsAsync_ActivityBelongsToOtherUser_ReturnsNull()
        {
            // Act - Try to get user 1's activity as user 2
            var result = await _service.GetActivityDetailsAsync(1, 2);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetActivityDetailsAsync_IncludesAllActivityDetails()
        {
            // Act
            var result = await _service.GetActivityDetailsAsync(3, 1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result!.Id);
            Assert.AreEqual(7200, result.DurationInSeconds);
            Assert.AreEqual(50000, result.DistanceInMeters);
            Assert.AreEqual(1200, result.TotalElevationGainInMeters);
            Assert.AreEqual(1500, result.ActiveKilocalories);
            Assert.AreEqual("Edge 830", result.DeviceName);
            Assert.IsFalse(result.IsManual);
            Assert.IsFalse(result.IsWebUpload);
        }
    }

    [TestFixture]
    public class CyclingActivityFilteringTests : GarminActivityProcessingServiceTests
    {
        [Test]
        public async Task GetCyclingActivitiesAsync_FiltersCyclingTypes()
        {
            // Arrange - Add different cycling types
            var mountainBikeActivity = new GarminActivity
            {
                Id = 10,
                UserId = 1,
                SummaryId = "mountain-bike",
                ActivityType = GarminActivityType.MOUNTAIN_BIKING,
                StartTime = DateTime.UtcNow.AddDays(-1),
                DurationInSeconds = 3600,
                ReceivedAt = DateTime.UtcNow.AddDays(-1),
                ResponseData = "{}"
            };

            var roadBikeActivity = new GarminActivity
            {
                Id = 11,
                UserId = 1,
                SummaryId = "road-bike",
                ActivityType = GarminActivityType.ROAD_BIKING,
                StartTime = DateTime.UtcNow.AddDays(-1),
                DurationInSeconds = 3600,
                ReceivedAt = DateTime.UtcNow.AddDays(-1),
                ResponseData = "{}"
            };

            var runningActivity = new GarminActivity
            {
                Id = 12,
                UserId = 1,
                SummaryId = "running",
                ActivityType = GarminActivityType.RUNNING,
                StartTime = DateTime.UtcNow.AddDays(-1),
                DurationInSeconds = 1800,
                ReceivedAt = DateTime.UtcNow.AddDays(-1),
                ResponseData = "{}"
            };

            _context.GarminActivities.AddRange(mountainBikeActivity, roadBikeActivity, runningActivity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCyclingActivitiesAsync(1, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);

            // Assert
            var cyclingTypes = result.Select(a => a.ActivityType).Distinct().ToList();
            
            // Should include cycling types but not running
            Assert.IsTrue(cyclingTypes.Contains(GarminActivityType.CYCLING));
            Assert.IsTrue(cyclingTypes.Contains(GarminActivityType.MOUNTAIN_BIKING));
            Assert.IsTrue(cyclingTypes.Contains(GarminActivityType.ROAD_BIKING));
            Assert.IsFalse(cyclingTypes.Contains(GarminActivityType.RUNNING));
        }
    }
}