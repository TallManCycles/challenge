using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using backend.Services;

namespace backend.Services;

public class GarminWebhookBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GarminWebhookBackgroundService> _logger;
    private readonly IServiceScopeFactory _seviceScopeFactory;

    public GarminWebhookBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<GarminWebhookBackgroundService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _seviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _seviceScopeFactory.CreateScope())
                {
                    //using var scope = _serviceProvider.CreateScope();
                    var webhookService = scope.ServiceProvider.GetRequiredService<IGarminWebhookService>();
                
                    await webhookService.ProcessStoredPayloadsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Garmin webhook background processing");
            }

            // Wait 5 minutes before next processing
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}