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
            // Still seed quotes if they don't exist
            await SeedQuotesIfNeeded();
            return;
        }
        
        const string testPassword = "Password123!";

        // Create test users
        var users = new List<User>
        {
            new User
            {
                Username = "alex_rivera",
                Email = "alex.rivera@example.com",
                FullName = "Alex Rivera",
                PasswordHash = HashPassword(testPassword),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                Username = "mike_johnson",
                Email = "mike.johnson@example.com",
                FullName = "Mike Johnson",
                PasswordHash = HashPassword(testPassword),
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new User
            {
                Username = "sarah_chen",
                Email = "sarah.chen@example.com",
                FullName = "Sarah Chen",
                PasswordHash = HashPassword(testPassword),
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

        // Seed quotes
        await SeedQuotesIfNeeded();
    }

    private async Task SeedQuotesIfNeeded()
    {
        // Check if quotes already exist
        var existingCount = await _context.Quotes.CountAsync();
        if (existingCount > 0)
        {
            return;
        }

        var quotes = GetQuotesData();
        
        _context.Quotes.AddRange(quotes);
        await _context.SaveChangesAsync();
    }

    private static List<Quote> GetQuotesData()
    {
        return new List<Quote>
        {
            // Training & Preparation
            new Quote { Text = "The race is won by the rider who can suffer the most.", Author = "Eddy Merckx", Category = "Training & Preparation" },
            new Quote { Text = "Pain is temporary. Quitting lasts forever.", Author = "Lance Armstrong", Category = "Training & Preparation" },
            new Quote { Text = "It never gets easier, you just go faster.", Author = "Greg LeMond", Category = "Training & Preparation" },
            new Quote { Text = "Cycling is a sport of the open road and spectators are lining that road.", Author = "Miguel Indurain", Category = "Training & Preparation" },
            new Quote { Text = "Training is like fighting with a gorilla. You don't stop when you're tired. You stop when the gorilla is tired.", Author = "Sean Kelly", Category = "Training & Preparation" },
            new Quote { Text = "Shut up legs!", Author = "Jens Voigt", Category = "Training & Preparation" },
            new Quote { Text = "I race to win, not to please people.", Author = "Alberto Contador", Category = "Training & Preparation" },
            new Quote { Text = "Cycling has given me everything. It's given me a career, it's given me friends, it's given me a way of life.", Author = "Chris Froome", Category = "Training & Preparation" },
            new Quote { Text = "The most important thing is to try and inspire people so that they can be great at whatever they want to do.", Author = "Marianne Vos", Category = "Training & Preparation" },
            new Quote { Text = "I am not a robot. I am a human being.", Author = "Peter Sagan", Category = "Training & Preparation" },

            // Mental Strength & Perseverance
            new Quote { Text = "If you want to be a successful cyclist, you have to be willing to suffer.", Author = "Bradley Wiggins", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "Cycling is unique, the only sport where you can be in the lead and still lose.", Author = "Cadel Evans", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "In cycling, you have to be mentally strong because physically everyone is at the same level.", Author = "Andy Schleck", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "Cycling teaches you that life has ups and downs, and you must be ready for both.", Author = "Vincenzo Nibali", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "You have to believe in yourself when no one else does.", Author = "Tom Boonen", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "Pain is weakness leaving the body.", Author = "Fabian Cancellara", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "The bicycle is a curious vehicle. Its passenger is its engine.", Author = "Ivan Basso", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "Cycling is meditation in motion.", Author = "Carlos Sastre", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "Every pedal stroke is a choice between giving up and pushing forward.", Author = "Alejandro Valverde", Category = "Mental Strength & Perseverance" },
            new Quote { Text = "Mountains don't care about your reputation, only your preparation.", Author = "Nairo Quintana", Category = "Mental Strength & Perseverance" },

            // Competition & Victory
            new Quote { Text = "I sprint because I want to win, not because I have to.", Author = "Mark Cavendish", Category = "Competition & Victory" },
            new Quote { Text = "Speed is not about the bike, it's about the mind.", Author = "Marcel Kittel", Category = "Competition & Victory" },
            new Quote { Text = "A sprinter without confidence is just a regular cyclist.", Author = "André Greipel", Category = "Competition & Victory" },
            new Quote { Text = "The sprint is 90% mental and 10% physical.", Author = "Robbie McEwen", Category = "Competition & Victory" },
            new Quote { Text = "Winning is not everything, but wanting to win is.", Author = "Erik Zabel", Category = "Competition & Victory" },
            new Quote { Text = "I don't ride a bike to add days to my life. I ride a bike to add life to my days.", Author = "Mario Cipollini", Category = "Competition & Victory" },
            new Quote { Text = "The finish line is just the beginning of a whole new race.", Author = "Mark Cavendish", Category = "Competition & Victory" },
            new Quote { Text = "Every race teaches you something about yourself.", Author = "Thor Hushovd", Category = "Competition & Victory" },
            new Quote { Text = "Attacking is my way of racing.", Author = "Philippe Gilbert", Category = "Competition & Victory" },
            new Quote { Text = "I race with my heart, not just my legs.", Author = "Julian Alaphilippe", Category = "Competition & Victory" },

            // Women's Cycling Champions
            new Quote { Text = "Don't limit yourself. Many people limit themselves to what they think they can do.", Author = "Marianne Vos", Category = "Women's Cycling Champions" },
            new Quote { Text = "Strength doesn't come from what you can do. It comes from overcoming the things you once thought you couldn't.", Author = "Anna van der Breggen", Category = "Women's Cycling Champions" },
            new Quote { Text = "Every race is a chance to prove yourself all over again.", Author = "Lizzie Deignan", Category = "Women's Cycling Champions" },
            new Quote { Text = "Age is just a number when you have passion in your heart.", Author = "Annemiek van Vleuten", Category = "Women's Cycling Champions" },
            new Quote { Text = "Cycling taught me that the only limits are the ones you accept.", Author = "Katrin Garfoot", Category = "Women's Cycling Champions" },
            new Quote { Text = "The bike doesn't care about your gender, only about your determination.", Author = "Kristin Armstrong", Category = "Women's Cycling Champions" },
            new Quote { Text = "Small in stature, giant in heart.", Author = "Emma Pooley", Category = "Women's Cycling Champions" },
            new Quote { Text = "Breaking barriers is what champions do.", Author = "Nicole Cooke", Category = "Women's Cycling Champions" },
            new Quote { Text = "Consistency is the mother of mastery.", Author = "Jeannie Longo", Category = "Women's Cycling Champions" },
            new Quote { Text = "Every pedal stroke is a step towards your dreams.", Author = "Leontien van Moorsel", Category = "Women's Cycling Champions" },

            // Philosophy & Life Lessons
            new Quote { Text = "Ride as much or as little, as long or as short as you feel. But ride.", Author = "Eddy Merckx", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "Cycling is a sport where you can't hide. Everything shows.", Author = "Greg LeMond", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "As long as I breathe, I attack.", Author = "Bernard Hinault", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "Cycling has taught me patience above all else.", Author = "Miguel Indurain", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "The road teaches you everything you need to know about life.", Author = "Sean Kelly", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "Every day on the bike is a good day.", Author = "Stephen Roche", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "Cycling is the closest thing to flying without leaving the ground.", Author = "Pedro Delgado", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "Hills are alive, they have a soul, and they test yours.", Author = "Robert Millar", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "The bike is a vehicle for dreams.", Author = "Phil Anderson", Category = "Philosophy & Life Lessons" },
            new Quote { Text = "Respect the bike, respect the road, respect yourself.", Author = "Cadel Evans", Category = "Philosophy & Life Lessons" },

            // Modern Era Motivation
            new Quote { Text = "Dream big, train hard, race harder.", Author = "Tadej Pogačar", Category = "Modern Era Motivation" },
            new Quote { Text = "Every challenge is an opportunity in disguise.", Author = "Jonas Vingegaard", Category = "Modern Era Motivation" },
            new Quote { Text = "Youth is not about age, it's about attitude.", Author = "Remco Evenepoel", Category = "Modern Era Motivation" },
            new Quote { Text = "Versatility is strength in cycling and in life.", Author = "Mathieu van der Poel", Category = "Modern Era Motivation" },
            new Quote { Text = "Adapt, overcome, and never stop pushing forward.", Author = "Wout van Aert", Category = "Modern Era Motivation" },
            new Quote { Text = "It's never too late to chase your dreams.", Author = "Primož Roglič", Category = "Modern Era Motivation" },
            new Quote { Text = "Innovation comes from those who dare to be different.", Author = "Tom Pidcock", Category = "Modern Era Motivation" },
            new Quote { Text = "Every race is a new story waiting to be written.", Author = "Mads Pedersen", Category = "Modern Era Motivation" },
            new Quote { Text = "Speed is earned through dedication and sacrifice.", Author = "Jasper Philipsen", Category = "Modern Era Motivation" },
            new Quote { Text = "Breaking new ground requires courage and unwavering belief.", Author = "Biniam Girmay", Category = "Modern Era Motivation" }
        };
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}