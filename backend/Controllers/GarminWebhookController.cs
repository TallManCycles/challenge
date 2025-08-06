using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GarminWebhookController : ControllerBase
{
    private readonly IGarminWebhookService _webhookService;
    private readonly IFileLoggingService _logger;

    public GarminWebhookController(
        IGarminWebhookService webhookService,
        IFileLoggingService logger)
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

            await _logger.LogInfoAsync($"Received ping webhook for type {webhookType}");

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
                    await _logger.LogErrorAsync("Error processing ping webhook asynchronously",ex,"GarminWebhookController");
                }
            });

            return Ok();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error handling ping webhook for type {webhookType}",  ex, "GarminWebhookController");
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

            await _logger.LogInfoAsync($"Received push webhook for type {webhookType}");

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
                    await _logger.LogErrorAsync("Error processing push webhook asynchronously", ex, "GarminWebhookController");
                }
            });

            return Ok();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error handling push webhook for type {webhookType}", ex, "GarminWebhookController");
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
            await _logger.LogErrorAsync("Error processing failed payloads", ex, "GarminWebhookController");
            return StatusCode(500, new { error = "Failed to process failed payloads" });
        }
    }
}