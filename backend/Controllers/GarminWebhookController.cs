using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GarminWebhookController : ControllerBase
{
    private readonly IGarminWebhookService _webhookService;
    private readonly ILogger<GarminWebhookController> _logger;

    public GarminWebhookController(
        IGarminWebhookService webhookService,
        ILogger<GarminWebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("ping/{webhookType}")]
    public async Task<IActionResult> HandlePingWebhook(string webhookType)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            string payload = await reader.ReadToEndAsync();

            _logger.LogInformation("Received ping webhook for type {WebhookType}", webhookType);

            // Immediately return 200 OK as required by Garmin
            var processingTask = _webhookService.ProcessPingNotificationAsync(webhookType, payload);
            
            // Don't await - process asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await processingTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ping webhook asynchronously");
                }
            });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ping webhook for type {WebhookType}", webhookType);
            return Ok(); // Still return 200 to avoid retries
        }
    }

    [HttpPost("push/{webhookType}")]
    public async Task<IActionResult> HandlePushWebhook(string webhookType)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            string payload = await reader.ReadToEndAsync();

            _logger.LogInformation("Received push webhook for type {WebhookType}", webhookType);

            // Immediately return 200 OK as required by Garmin
            var processingTask = _webhookService.ProcessPushNotificationAsync(webhookType, payload);
            
            // Don't await - process asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await processingTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing push webhook asynchronously");
                }
            });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling push webhook for type {WebhookType}", webhookType);
            return Ok(); // Still return 200 to avoid retries
        }
    }

    // Endpoint for manual processing of failed payloads
    [HttpPost("process-failed")]
    public async Task<IActionResult> ProcessFailedPayloads()
    {
        try
        {
            await _webhookService.ProcessStoredPayloadsAsync();
            return Ok(new { message = "Failed payloads processing initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed payloads");
            return StatusCode(500, new { error = "Failed to process failed payloads" });
        }
    }
}