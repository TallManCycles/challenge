using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Moq;
using backend.Data;
using backend.Services;
using Microsoft.Extensions.Hosting;

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
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.SetupGet(env => env.EnvironmentName).Returns("Production");
        return new FileLoggingService(mockEnvironment.Object);
    }
}