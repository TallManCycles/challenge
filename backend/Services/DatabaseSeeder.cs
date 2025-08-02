using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.Models;

namespace backend.Services;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;

    public DatabaseSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // Skip seeding if data already exists
        if (await _context.Users.AnyAsync())
        {
            return;
        }

        // Create test users
        var users = new List<User>
        {
            new User
            {
                Username = "alex_rivera",
                Email = "alex.rivera@example.com",
                FullName = "Alex Rivera",
                PasswordHash = HashPassword("password123"),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                Username = "mike_johnson",
                Email = "mike.johnson@example.com",
                FullName = "Mike Johnson",
                PasswordHash = HashPassword("password123"),
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new User
            {
                Username = "sarah_chen",
                Email = "sarah.chen@example.com",
                FullName = "Sarah Chen",
                PasswordHash = HashPassword("password123"),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Create test challenges
        var challenges = new List<Challenge>
        {
            new Challenge
            {
                Title = "100 Mile Challenge",
                Description = "Complete 100 miles of cycling in 4 weeks",
                CreatedById = users[0].Id, // Alex Rivera
                ChallengeType = ChallengeType.Distance,
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow.AddDays(14),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14)
            },
            new Challenge
            {
                Title = "Mountain Climber",
                Description = "Gain 10,000 feet of elevation in 3 weeks",
                CreatedById = users[1].Id, // Mike Johnson
                ChallengeType = ChallengeType.Elevation,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(11),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Challenge
            {
                Title = "Time Trial Masters",
                Description = "Accumulate 20 hours of cycling time",
                CreatedById = users[2].Id, // Sarah Chen
                ChallengeType = ChallengeType.Time,
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddDays(21),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            }
        };

        await _context.Challenges.AddRangeAsync(challenges);
        await _context.SaveChangesAsync();

        // Create challenge participants
        var participants = new List<ChallengeParticipant>
        {
            // 100 Mile Challenge participants
            new ChallengeParticipant { ChallengeId = challenges[0].Id, UserId = users[0].Id, CurrentTotal = 95.3m, JoinedAt = DateTime.UtcNow.AddDays(-14) },
            new ChallengeParticipant { ChallengeId = challenges[0].Id, UserId = users[1].Id, CurrentTotal = 87.6m, JoinedAt = DateTime.UtcNow.AddDays(-13) },
            new ChallengeParticipant { ChallengeId = challenges[0].Id, UserId = users[2].Id, CurrentTotal = 73.2m, JoinedAt = DateTime.UtcNow.AddDays(-12) },

            // Mountain Climber participants
            new ChallengeParticipant { ChallengeId = challenges[1].Id, UserId = users[1].Id, CurrentTotal = 8500m, JoinedAt = DateTime.UtcNow.AddDays(-10) },
            new ChallengeParticipant { ChallengeId = challenges[1].Id, UserId = users[0].Id, CurrentTotal = 7200m, JoinedAt = DateTime.UtcNow.AddDays(-9) },
            new ChallengeParticipant { ChallengeId = challenges[1].Id, UserId = users[2].Id, CurrentTotal = 6800m, JoinedAt = DateTime.UtcNow.AddDays(-8) },

            // Time Trial Masters participants
            new ChallengeParticipant { ChallengeId = challenges[2].Id, UserId = users[2].Id, CurrentTotal = 16.5m, JoinedAt = DateTime.UtcNow.AddDays(-7) },
            new ChallengeParticipant { ChallengeId = challenges[2].Id, UserId = users[0].Id, CurrentTotal = 14.2m, JoinedAt = DateTime.UtcNow.AddDays(-6) },
            new ChallengeParticipant { ChallengeId = challenges[2].Id, UserId = users[1].Id, CurrentTotal = 12.8m, JoinedAt = DateTime.UtcNow.AddDays(-5) }
        };

        await _context.ChallengeParticipants.AddRangeAsync(participants);
        await _context.SaveChangesAsync();

        // Create 20 activities
        var activities = new List<Activity>();
        var random = new Random();
        var activityTypes = new[] { "Morning ride", "Evening commute", "Weekend adventure", "Lunch break ride", "Training session", "Recovery ride" };

        for (int i = 0; i < 20; i++)
        {
            var user = users[i % 3];
            var daysAgo = random.Next(1, 14);
            var distance = Math.Round((decimal)(random.NextDouble() * 25 + 5), 1); // 5-30 miles
            var elevation = Math.Round((decimal)(random.NextDouble() * 1500 + 100), 0); // 100-1600 feet
            var time = random.Next(1800, 7200); // 30 minutes to 2 hours in seconds
            
            activities.Add(new Activity
            {
                UserId = user.Id,
                GarminActivityId = Guid.NewGuid().ToString(),
                ActivityName = activityTypes[random.Next(activityTypes.Length)],
                Distance = distance,
                ElevationGain = elevation,
                MovingTime = time,
                ActivityDate = DateTime.UtcNow.AddDays(-daysAgo),
                CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
            });
        }

        await _context.Activities.AddRangeAsync(activities);
        await _context.SaveChangesAsync();

        // Update LastActivityDate for participants
        foreach (var participant in participants)
        {
            var lastActivity = activities
                .Where(a => a.UserId == participant.UserId)
                .OrderByDescending(a => a.ActivityDate)
                .FirstOrDefault();
            
            if (lastActivity != null)
            {
                participant.LastActivityDate = lastActivity.ActivityDate;
            }
        }

        await _context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        // Simple password hashing - in production, use a proper hashing library like BCrypt
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}