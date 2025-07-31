using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using backend.Data;
using backend.Models;

namespace backend.Tests;

[TestFixture]
public class DatabaseIntegrationTests
{
    private ApplicationDbContext _context;
    private DbContextOptions<ApplicationDbContext> _options;

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(_options);
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task User_CreateUpdateSelect_ShouldWorkCorrectly()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Create
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert - Create
        Assert.That(user.Id, Is.GreaterThan(0));
        
        // Act - Select
        var retrievedUser = await _context.Users.FindAsync(user.Id);
        
        // Assert - Select
        Assert.That(retrievedUser, Is.Not.Null);
        Assert.That(retrievedUser.Email, Is.EqualTo("test@example.com"));
        Assert.That(retrievedUser.Username, Is.EqualTo("testuser"));

        // Act - Update
        retrievedUser.Username = "updateduser";
        retrievedUser.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act - Select updated
        var updatedUser = await _context.Users.FindAsync(user.Id);

        // Assert - Update
        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser.Username, Is.EqualTo("updateduser"));
        Assert.That(updatedUser.Email, Is.EqualTo("test@example.com"));
    }
}