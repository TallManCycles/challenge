using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using backend.Data;
using backend.Services;

namespace backend.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(string databaseName = "")
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = Guid.NewGuid().ToString();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IConfiguration CreateTestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:JwtKey"] = "TestJwtKey123456789012345678901234567890",
                ["Auth:Salt"] = "TestSalt123!"
            })
            .Build();

        return configuration;
    }

    public static IFileLoggingService CreateTestLogger()
    {
        return new FileLoggingService();
    }
}