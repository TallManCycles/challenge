using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class GarminDailyActivityBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;

    public GarminDailyActivityBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the application has fully started before beginning execution
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        // Run an initial fetch on startup to catch any activities missed while service was down
        var runOnStartup = _configuration.GetValue<bool>("GarminDailyFetch:RunOnStartup", true);
        if (runOnStartup && !stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                await logger.LogInfoAsync("Running initial daily activity fetch on startup");
            }
            await ExecuteDailyFetchAsync();
        }
        else if (!runOnStartup)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                await logger.LogInfoAsync("Startup fetch disabled by configuration");
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
                    await logger.LogInfoAsync($"Next daily activity fetch scheduled for {nextRunTime} UTC");
                }
                
                // Wait until the next scheduled run time
                var delay = nextRunTime - now;
                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                // Execute the daily fetch
                if (!stoppingToken.IsCancellationRequested)
                {
                    await ExecuteDailyFetchAsync();
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
                    await logger.LogErrorAsync("Error in Garmin daily activity background service", ex, "GarminDailyActivityBackgroundService");
                }
                
                // Wait 1 hour before retrying on error to avoid hammering the API
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task ExecuteDailyFetchAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
            await logger.LogInfoAsync("Starting daily Garmin activity fetch");

            var dailyFetchService = scope.ServiceProvider.GetRequiredService<IGarminDailyActivityFetchService>();
            
            await dailyFetchService.FetchActivitiesForAllUsersAsync();
            
            await logger.LogInfoAsync("Completed daily Garmin activity fetch");
        }
        catch (Exception ex)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
            await logger.LogErrorAsync("Error during daily Garmin activity fetch execution", ex, "GarminDailyActivityBackgroundService");
        }
    }

    private static DateTime GetNextRunTime(DateTime currentTime)
    {
        // Run daily at 02:00 UTC (early morning to catch activities from the previous day)
        var targetTime = new TimeOnly(2, 0);
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