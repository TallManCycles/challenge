using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class ChallengeCompletionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;

    public ChallengeCompletionBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the application has fully started before beginning execution
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        // Run an initial check on startup to catch any challenges that completed while service was down
        var runOnStartup = _configuration.GetValue<bool>("ChallengeCompletion:RunOnStartup", true);
        if (runOnStartup && !stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                await logger.LogInfoAsync("Running initial challenge completion check on startup");
            }
            await ExecuteChallengeCompletionCheckAsync();
        }
        else if (!runOnStartup)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                await logger.LogInfoAsync("Startup challenge completion check disabled by configuration");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRunTime = GetNextRunTime(now);
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                    await logger.LogInfoAsync($"Next challenge completion check scheduled for {nextRunTime} UTC");
                }
                
                // Wait until the next scheduled run time
                var delay = nextRunTime - now;
                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                // Execute the challenge completion check
                if (!stoppingToken.IsCancellationRequested)
                {
                    await ExecuteChallengeCompletionCheckAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the service
                break;
            }
            catch (Exception ex)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                    await logger.LogErrorAsync("Error in challenge completion background service", ex, "ChallengeCompletionBackgroundService");
                }
                
                // Wait 1 hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task ExecuteChallengeCompletionCheckAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
            await logger.LogInfoAsync("Starting challenge completion check");

            var notificationService = scope.ServiceProvider.GetRequiredService<IChallengeNotificationService>();
            
            await notificationService.CheckAndNotifyCompletedChallengesAsync();
            
            await logger.LogInfoAsync("Completed challenge completion check");
        }
        catch (Exception ex)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
            await logger.LogErrorAsync("Error during challenge completion check execution", ex, "ChallengeCompletionBackgroundService");
        }
    }

    private static DateTime GetNextRunTime(DateTime currentTime)
    {
        // Run daily at 00:30 UTC (30 minutes after midnight to catch challenges that ended at midnight)
        var targetTime = new TimeOnly(0, 30);
        var today = DateOnly.FromDateTime(currentTime);
        var todayTarget = today.ToDateTime(targetTime);

        // If we've already passed today's target time, schedule for tomorrow
        if (currentTime >= todayTarget)
        {
            return today.AddDays(1).ToDateTime(targetTime);
        }
        
        return todayTarget;
    }
}