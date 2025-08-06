using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Tests.Services;

[TestFixture]
public class QuoteServiceTests
{
    private ServiceProvider _serviceProvider = null!;
    private ApplicationDbContext _context = null!;
    private QuoteService _quoteService = null!;
    private Mock<ILogger<QuoteService>> _mockLogger = null!;
    private string _databaseName = null!;

    [SetUp]
    public void SetUp()
    {
        _databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        
        // Configure in-memory database with consistent name
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: _databaseName));
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        _mockLogger = new Mock<ILogger<QuoteService>>();
        _quoteService = new QuoteService(_serviceProvider, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task GetRandomQuoteAsync_WithActiveQuotes_ReturnsRandomQuote()
    {
        // Arrange
        var quotes = new List<Quote>
        {
            new Quote { Text = "Quote 1", Author = "Author 1", Category = "Test", IsActive = true },
            new Quote { Text = "Quote 2", Author = "Author 2", Category = "Test", IsActive = true },
            new Quote { Text = "Quote 3", Author = "Author 3", Category = "Test", IsActive = false }
        };
        
        _context.Quotes.AddRange(quotes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.GetRandomQuoteAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsActive, Is.True);
        Assert.That(new[] { "Quote 1", "Quote 2" }, Contains.Item(result.Text));
    }

    [Test]
    public async Task GetRandomQuoteAsync_WithNoActiveQuotes_ReturnsNull()
    {
        // Arrange
        var inactiveQuote = new Quote 
        { 
            Text = "Inactive Quote", 
            Author = "Author", 
            Category = "Test", 
            IsActive = false 
        };
        
        _context.Quotes.Add(inactiveQuote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.GetRandomQuoteAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetRandomQuoteAsync_WithEmptyDatabase_ReturnsNull()
    {
        // Act
        var result = await _quoteService.GetRandomQuoteAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetRandomQuoteByCategoryAsync_WithCategoryQuotes_ReturnsQuoteFromCategory()
    {
        // Arrange
        var quotes = new List<Quote>
        {
            new Quote { Text = "Training Quote", Author = "Author 1", Category = "Training", IsActive = true },
            new Quote { Text = "Motivation Quote", Author = "Author 2", Category = "Motivation", IsActive = true },
            new Quote { Text = "Another Training Quote", Author = "Author 3", Category = "Training", IsActive = true }
        };
        
        _context.Quotes.AddRange(quotes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.GetRandomQuoteByCategoryAsync("Training");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Category, Is.EqualTo("Training"));
        Assert.That(new[] { "Training Quote", "Another Training Quote" }, Contains.Item(result.Text));
    }

    [Test]
    public async Task GetRandomQuoteByCategoryAsync_WithNonExistentCategory_ReturnsNull()
    {
        // Arrange
        var quote = new Quote 
        { 
            Text = "Training Quote", 
            Author = "Author", 
            Category = "Training", 
            IsActive = true 
        };
        
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.GetRandomQuoteByCategoryAsync("NonExistent");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllQuotesAsync_ReturnsOnlyActiveQuotes()
    {
        // Arrange
        var quotes = new List<Quote>
        {
            new Quote { Text = "Active Quote 1", Author = "Author 1", Category = "Test", IsActive = true },
            new Quote { Text = "Active Quote 2", Author = "Author 2", Category = "Test", IsActive = true },
            new Quote { Text = "Inactive Quote", Author = "Author 3", Category = "Test", IsActive = false }
        };
        
        _context.Quotes.AddRange(quotes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.GetAllQuotesAsync();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(q => q.IsActive), Is.True);
        Assert.That(result.Select(q => q.Text), Does.Not.Contain("Inactive Quote"));
    }

    [Test]
    public async Task GetQuoteByIdAsync_WithValidId_ReturnsQuote()
    {
        // Arrange
        var quote = new Quote 
        { 
            Text = "Test Quote", 
            Author = "Test Author", 
            Category = "Test", 
            IsActive = true 
        };
        
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.GetQuoteByIdAsync(quote.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Text, Is.EqualTo("Test Quote"));
        Assert.That(result.Author, Is.EqualTo("Test Author"));
    }

    [Test]
    public async Task GetQuoteByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _quoteService.GetQuoteByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddQuoteAsync_ValidQuote_AddsAndReturnsQuote()
    {
        // Arrange
        var newQuote = new Quote 
        { 
            Text = "New Quote", 
            Author = "New Author", 
            Category = "New Category", 
            IsActive = true 
        };

        // Act
        var result = await _quoteService.AddQuoteAsync(newQuote);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.Text, Is.EqualTo("New Quote"));
        
        // Verify in a fresh context since the service creates its own scope
        using var scope = _serviceProvider.CreateScope();
        using var freshContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedQuote = await freshContext.Quotes.FindAsync(result.Id);
        Assert.That(savedQuote, Is.Not.Null);
        Assert.That(savedQuote!.Text, Is.EqualTo("New Quote"));
    }

    [Test]
    public async Task UpdateQuoteAsync_ValidId_UpdatesAndReturnsQuote()
    {
        // Arrange
        var existingQuote = new Quote 
        { 
            Text = "Original Text", 
            Author = "Original Author", 
            Category = "Original", 
            IsActive = true 
        };
        
        _context.Quotes.Add(existingQuote);
        await _context.SaveChangesAsync();
        var existingId = existingQuote.Id;

        var updateQuote = new Quote 
        { 
            Text = "Updated Text", 
            Author = "Updated Author", 
            Category = "Updated", 
            IsActive = false 
        };

        // Act
        var result = await _quoteService.UpdateQuoteAsync(existingId, updateQuote);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Text, Is.EqualTo("Updated Text"));
        Assert.That(result.Author, Is.EqualTo("Updated Author"));
        Assert.That(result.Category, Is.EqualTo("Updated"));
        Assert.That(result.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateQuoteAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var updateQuote = new Quote 
        { 
            Text = "Updated Text", 
            Author = "Updated Author", 
            Category = "Updated", 
            IsActive = false 
        };

        // Act
        var result = await _quoteService.UpdateQuoteAsync(999, updateQuote);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]  
    public async Task DeleteQuoteAsync_ValidId_DeletesQuoteAndReturnsTrue()
    {
        // Arrange
        var quote = new Quote 
        { 
            Text = "Quote to Delete", 
            Author = "Author", 
            Category = "Test", 
            IsActive = true 
        };
        
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();
        var quoteId = quote.Id;

        // Act
        var result = await _quoteService.DeleteQuoteAsync(quoteId);

        // Assert
        Assert.That(result, Is.True);
        
        // Verify in a fresh context since the service creates its own scope
        using var scope = _serviceProvider.CreateScope();
        using var freshContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedQuote = await freshContext.Quotes.FindAsync(quoteId);
        Assert.That(deletedQuote, Is.Null);
    }

    [Test]
    public async Task DeleteQuoteAsync_InvalidId_ReturnsFalse()
    {
        // Act
        var result = await _quoteService.DeleteQuoteAsync(999);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task SeedQuotesAsync_WithEmptyDatabase_SeedsQuotesAndReturnsCount()
    {
        // Act
        var result = await _quoteService.SeedQuotesAsync();

        // Assert
        Assert.That(result, Is.GreaterThan(0));
        
        // Verify in a fresh context since the service creates its own scope
        using var scope = _serviceProvider.CreateScope();
        using var freshContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var quotesInDb = await freshContext.Quotes.CountAsync();
        Assert.That(quotesInDb, Is.EqualTo(result));
        Assert.That(quotesInDb, Is.GreaterThan(50)); // Should have many quotes from the seed data
    }

    [Test]
    public async Task SeedQuotesAsync_WithExistingQuotes_SkipsSeedingAndReturnsZero()
    {
        // Arrange
        var existingQuote = new Quote 
        { 
            Text = "Existing Quote", 
            Author = "Author", 
            Category = "Test", 
            IsActive = true 
        };
        
        _context.Quotes.Add(existingQuote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quoteService.SeedQuotesAsync();

        // Assert
        Assert.That(result, Is.EqualTo(0));
        
        // Verify in a fresh context since the service creates its own scope
        using var scope = _serviceProvider.CreateScope();
        using var freshContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var quotesInDb = await freshContext.Quotes.CountAsync();
        Assert.That(quotesInDb, Is.EqualTo(1)); // Only the existing quote
    }

    [Test]
    public async Task SeedQuotesAsync_VerifiesQuoteContent()
    {
        // Act
        await _quoteService.SeedQuotesAsync();

        // Assert - Use fresh context since the service creates its own scope
        using var scope = _serviceProvider.CreateScope();
        using var freshContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var edgyMerckxQuote = await freshContext.Quotes
            .FirstOrDefaultAsync(q => q.Author == "Eddy Merckx" && q.Text.Contains("suffer"));
        
        Assert.That(edgyMerckxQuote, Is.Not.Null);
        Assert.That(edgyMerckxQuote!.Category, Is.EqualTo("Training & Preparation"));
        Assert.That(edgyMerckxQuote.IsActive, Is.True);

        var femaleQuote = await freshContext.Quotes
            .FirstOrDefaultAsync(q => q.Category == "Women's Cycling Champions");
        
        Assert.That(femaleQuote, Is.Not.Null);
    }

    [Test]
    public async Task ConcurrentAccess_MultipleQuoteRetrievals_DoesNotThrowConcurrencyException()
    {
        // Arrange
        var quotes = new List<Quote>();
        for (int i = 0; i < 10; i++)
        {
            quotes.Add(new Quote 
            { 
                Text = $"Quote {i}", 
                Author = $"Author {i}", 
                Category = "Test", 
                IsActive = true 
            });
        }
        
        _context.Quotes.AddRange(quotes);
        await _context.SaveChangesAsync();

        // Act & Assert - Should not throw any concurrency exceptions
        var tasks = new List<Task<Quote?>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_quoteService.GetRandomQuoteAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results.All(r => r != null), Is.True);
        Assert.That(results.All(r => r!.IsActive), Is.True);
    }
}