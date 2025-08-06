using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using backend.Services;

namespace backend.Services;

public class GarminWebhookBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GarminWebhookBackgroundService(
        IServiceProvider serviceProvider,
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceProvider = serviceProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var webhookService = scope.ServiceProvider.GetRequiredService<IGarminWebhookService>();
                    var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                
                    await webhookService.ProcessStoredPayloadsAsync();
                }
            }
            catch (Exception ex)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
                    await logger.LogErrorAsync("Error in Garmin webhook background processing", ex, "GarminWebhookBackgroundService");
                }
            }

            // Wait 5 minutes before next processing
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}