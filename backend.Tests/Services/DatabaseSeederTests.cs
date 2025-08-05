using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Tests.Services;

[TestFixture]
public class DatabaseSeederTests
{
    private ApplicationDbContext _context = null!;
    private DatabaseSeeder _seeder = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _seeder = new DatabaseSeeder(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task SeedAsync_WithEmptyDatabase_SeedsAllData()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        var challenges = await _context.Challenges.ToListAsync();
        var participants = await _context.ChallengeParticipants.ToListAsync();
        var activities = await _context.Activities.ToListAsync();
        var quotes = await _context.Quotes.ToListAsync();

        Assert.That(users.Count, Is.EqualTo(3));
        Assert.That(challenges.Count, Is.EqualTo(3));
        Assert.That(participants.Count, Is.EqualTo(9)); // 3 challenges × 3 participants each
        Assert.That(activities.Count, Is.EqualTo(20));
        Assert.That(quotes.Count, Is.GreaterThan(50)); // Should have many quotes from seed data
    }

    [Test]
    public async Task SeedAsync_WithExistingUsers_SkipsUserSeedingButStillSeedsQuotes()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existing_user",
            Email = "existing@example.com",
            FullName = "Existing User",
            PasswordHash = "hashed_password"
        };
        
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        var quotes = await _context.Quotes.ToListAsync();

        Assert.That(users.Count, Is.EqualTo(1)); // Only the existing user
        Assert.That(users.First().Username, Is.EqualTo("existing_user"));
        Assert.That(quotes.Count, Is.GreaterThan(50)); // Quotes should still be seeded
    }

    [Test]
    public async Task SeedAsync_WithExistingQuotes_SkipsQuoteSeeding()
    {
        // Arrange
        var existingQuote = new Quote
        {
            Text = "Existing quote",
            Author = "Existing Author",
            Category = "Test",
            IsActive = true
        };
        
        _context.Quotes.Add(existingQuote);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var quotes = await _context.Quotes.ToListAsync();
        Assert.That(quotes.Count, Is.EqualTo(1)); // Only the existing quote
        Assert.That(quotes.First().Text, Is.EqualTo("Existing quote"));
    }

    [Test]
    public async Task SeedAsync_CreatesUsersWithCorrectData()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        
        var alexRivera = users.FirstOrDefault(u => u.Username == "alex_rivera");
        Assert.That(alexRivera, Is.Not.Null);
        Assert.That(alexRivera!.Email, Is.EqualTo("alex.rivera@example.com"));
        Assert.That(alexRivera.FullName, Is.EqualTo("Alex Rivera"));
        Assert.That(BCrypt.Net.BCrypt.Verify("Password123!", alexRivera.PasswordHash), Is.True);

        var mikeJohnson = users.FirstOrDefault(u => u.Username == "mike_johnson");
        Assert.That(mikeJohnson, Is.Not.Null);
        Assert.That(mikeJohnson!.Email, Is.EqualTo("mike.johnson@example.com"));
        
        var sarahChen = users.FirstOrDefault(u => u.Username == "sarah_chen");
        Assert.That(sarahChen, Is.Not.Null);
        Assert.That(sarahChen!.Email, Is.EqualTo("sarah.chen@example.com"));
    }

    [Test]
    public async Task SeedAsync_CreatesChallengesWithCorrectTypes()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var challenges = await _context.Challenges.Include(c => c.CreatedBy).ToListAsync();
        
        var distanceChallenge = challenges.FirstOrDefault(c => c.Title == "100 Mile Challenge");
        Assert.That(distanceChallenge, Is.Not.Null);
        Assert.That(distanceChallenge!.ChallengeType, Is.EqualTo(ChallengeType.Distance));
        Assert.That(distanceChallenge.Description, Does.Contain("100 miles"));
        Assert.That(distanceChallenge.IsActive, Is.True);

        var elevationChallenge = challenges.FirstOrDefault(c => c.Title == "Mountain Climber");
        Assert.That(elevationChallenge, Is.Not.Null);
        Assert.That(elevationChallenge!.ChallengeType, Is.EqualTo(ChallengeType.Elevation));
        Assert.That(elevationChallenge.Description, Does.Contain("10,000 feet"));

        var timeChallenge = challenges.FirstOrDefault(c => c.Title == "Time Trial Masters");
        Assert.That(timeChallenge, Is.Not.Null);
        Assert.That(timeChallenge!.ChallengeType, Is.EqualTo(ChallengeType.Time));
        Assert.That(timeChallenge.Description, Does.Contain("20 hours"));
    }

    [Test]
    public async Task SeedAsync_CreatesParticipantsWithProgress()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var participants = await _context.ChallengeParticipants
            .Include(cp => cp.Challenge)
            .Include(cp => cp.User)
            .ToListAsync();

        Assert.That(participants.Count, Is.EqualTo(9));
        Assert.That(participants.All(p => p.CurrentTotal > 0), Is.True);
        Assert.That(participants.All(p => p.JoinedAt <= DateTime.UtcNow), Is.True);

        // Check that each challenge has 3 participants
        var challengeGroups = participants.GroupBy(p => p.ChallengeId);
        Assert.That(challengeGroups.Count(), Is.EqualTo(3));
        Assert.That(challengeGroups.All(g => g.Count() == 3), Is.True);
    }

    [Test]
    public async Task SeedAsync_CreatesActivitiesWithVariedData()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var activities = await _context.Activities.Include(a => a.User).ToListAsync();
        
        Assert.That(activities.Count, Is.EqualTo(20));
        Assert.That(activities.All(a => a.Distance > 0), Is.True);
        Assert.That(activities.All(a => a.ElevationGain > 0), Is.True);
        Assert.That(activities.All(a => a.MovingTime > 0), Is.True);
        Assert.That(activities.All(a => !string.IsNullOrEmpty(a.GarminActivityId)), Is.True);
        Assert.That(activities.All(a => !string.IsNullOrEmpty(a.ActivityName)), Is.True);
        Assert.That(activities.All(a => a.ActivityDate <= DateTime.UtcNow), Is.True);

        // Check that activities are distributed among users
        var userGroups = activities.GroupBy(a => a.UserId);
        Assert.That(userGroups.Count(), Is.GreaterThan(1));
    }

    [Test]
    public async Task SeedAsync_SeedsQuotesWithCorrectCategories()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var quotes = await _context.Quotes.ToListAsync();
        
        Assert.That(quotes.Count, Is.GreaterThan(50));
        Assert.That(quotes.All(q => q.IsActive), Is.True);
        Assert.That(quotes.All(q => !string.IsNullOrEmpty(q.Text)), Is.True);
        Assert.That(quotes.All(q => !string.IsNullOrEmpty(q.Author)), Is.True);
        Assert.That(quotes.All(q => !string.IsNullOrEmpty(q.Category)), Is.True);

        // Check for expected categories
        var categories = quotes.Select(q => q.Category).Distinct().ToList();
        Assert.That(categories, Contains.Item("Training & Preparation"));
        Assert.That(categories, Contains.Item("Mental Strength & Perseverance"));
        Assert.That(categories, Contains.Item("Competition & Victory"));
        Assert.That(categories, Contains.Item("Women's Cycling Champions"));
        Assert.That(categories, Contains.Item("Philosophy & Life Lessons"));
        Assert.That(categories, Contains.Item("Modern Era Motivation"));

        // Check for specific famous quotes
        var edgyMerckxQuote = quotes.FirstOrDefault(q => q.Author == "Eddy Merckx" && q.Text.Contains("suffer"));
        Assert.That(edgyMerckxQuote, Is.Not.Null);

        var gregLeMondQuote = quotes.FirstOrDefault(q => q.Author == "Greg LeMond" && q.Text.Contains("never gets easier"));
        Assert.That(gregLeMondQuote, Is.Not.Null);

        var mariannVosQuote = quotes.FirstOrDefault(q => q.Author == "Marianne Vos");
        Assert.That(mariannVosQuote, Is.Not.Null);
    }

    [Test]
    public async Task SeedAsync_UpdatesParticipantLastActivityDate()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var participants = await _context.ChallengeParticipants.ToListAsync();
        var activities = await _context.Activities.ToListAsync();

        foreach (var participant in participants)
        {
            var userActivities = activities.Where(a => a.UserId == participant.UserId).ToList();
            if (userActivities.Any())
            {
                var expectedLastActivity = userActivities.Max(a => a.ActivityDate);
                Assert.That(participant.LastActivityDate, Is.EqualTo(expectedLastActivity));
            }
        }
    }

    [Test]
    public async Task SeedAsync_EnsuresDataConsistency()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        var challenges = await _context.Challenges.Include(c => c.CreatedBy).ToListAsync();
        var participants = await _context.ChallengeParticipants
            .Include(cp => cp.User)
            .Include(cp => cp.Challenge)
            .ToListAsync();
        var activities = await _context.Activities.Include(a => a.User).ToListAsync();

        // Ensure all challenges have valid creators
        Assert.That(challenges.All(c => c.CreatedBy != null), Is.True);
        Assert.That(challenges.All(c => users.Any(u => u.Id == c.CreatedById)), Is.True);

        // Ensure all participants reference valid users and challenges
        Assert.That(participants.All(p => p.User != null), Is.True);
        Assert.That(participants.All(p => p.Challenge != null), Is.True);

        // Ensure all activities reference valid users
        Assert.That(activities.All(a => a.User != null), Is.True);
        Assert.That(activities.All(a => users.Any(u => u.Id == a.UserId)), Is.True);
    }

    [Test]
    public async Task SeedAsync_CreatesReasonableActivityValues()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var activities = await _context.Activities.ToListAsync();

        // Check that activity values are within reasonable ranges
        Assert.That(activities.All(a => a.Distance >= 5 && a.Distance <= 30), Is.True); // 5-30 miles
        Assert.That(activities.All(a => a.ElevationGain >= 100 && a.ElevationGain <= 1600), Is.True); // 100-1600 feet
        Assert.That(activities.All(a => a.MovingTime >= 1800 && a.MovingTime <= 7200), Is.True); // 30 min - 2 hours

        // Check that activities have varied dates within the last 14 days
        var oldestActivity = activities.Min(a => a.ActivityDate);
        var newestActivity = activities.Max(a => a.ActivityDate);
        var daysBetween = (newestActivity - oldestActivity).TotalDays;
        
        Assert.That(daysBetween, Is.GreaterThan(0));
        Assert.That(daysBetween, Is.LessThanOrEqualTo(14));
    }
}