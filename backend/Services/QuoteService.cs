using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class QuoteService : IQuoteService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(IServiceProvider serviceProvider, ILogger<QuoteService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Quote?> GetRandomQuoteAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var activeQuotes = await context.Quotes.Where(q => q.IsActive).ToListAsync();
        if (!activeQuotes.Any())
        {
            return null;
        }

        var random = new Random();
        var randomIndex = random.Next(activeQuotes.Count);
        return activeQuotes[randomIndex];
    }

    public async Task<Quote?> GetRandomQuoteByCategoryAsync(string category)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var activeQuotes = await context.Quotes
            .Where(q => q.IsActive && q.Category == category)
            .ToListAsync();
            
        if (!activeQuotes.Any())
        {
            return null;
        }

        var random = new Random();
        var randomIndex = random.Next(activeQuotes.Count);
        return activeQuotes[randomIndex];
    }

    public async Task<List<Quote>> GetAllQuotesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        return await context.Quotes.Where(q => q.IsActive).ToListAsync();
    }

    public async Task<Quote?> GetQuoteByIdAsync(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        return await context.Quotes.FindAsync(id);
    }

    public async Task<Quote> AddQuoteAsync(Quote quote)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.Quotes.Add(quote);
        await context.SaveChangesAsync();
        return quote;
    }

    public async Task<Quote?> UpdateQuoteAsync(int id, Quote quote)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var existingQuote = await context.Quotes.FindAsync(id);
        if (existingQuote == null)
        {
            return null;
        }

        existingQuote.Text = quote.Text;
        existingQuote.Author = quote.Author;
        existingQuote.Category = quote.Category;
        existingQuote.IsActive = quote.IsActive;

        await context.SaveChangesAsync();
        return existingQuote;
    }

    public async Task<bool> DeleteQuoteAsync(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var quote = await context.Quotes.FindAsync(id);
        if (quote == null)
        {
            return false;
        }

        context.Quotes.Remove(quote);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> SeedQuotesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Check if quotes already exist
        var existingCount = await context.Quotes.CountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("Quotes already exist in database. Skipping seed.");
            return 0;
        }

        var quotes = GetQuotesData();
        
        context.Quotes.AddRange(quotes);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Seeded {Count} quotes into database", quotes.Count);
        return quotes.Count;
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
            new Quote { Text = "The finish line is just the beginning of a whole new race.", Author = "Cavendish Marshall", Category = "Competition & Victory" },
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
}