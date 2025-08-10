using System.Collections.Concurrent;

namespace backend.Services;

public interface IFitFileQueue
{
    void QueueFileForProcessing(byte[] fileContent, string fileName);
}

public class FitFileBackgroundService : BackgroundService, IFitFileQueue
{
    private readonly ILogger<FitFileBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<(byte[] FileContent, string FileName)> _fileQueue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FitFileBackgroundService(
        ILogger<FitFileBackgroundService> logger, 
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void QueueFileForProcessing(byte[] fileContent, string fileName)
    {
        _fileQueue.Enqueue((fileContent, fileName));
        _logger.LogInformation("Queued FIT file for processing: {fileName}", fileName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FIT File Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_fileQueue.TryDequeue(out var fileInfo))
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        await ProcessFileAsync(fileInfo.FileContent, fileInfo.FileName);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    // No files to process, wait a bit before checking again
                    await Task.Delay(10000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FIT file background service");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("FIT File Background Service stopped");
    }

    private async Task ProcessFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            _logger.LogInformation("Processing FIT file: {fileName}", fileName);

            using var scope = _serviceProvider.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<IFitFileProcessingService>();

            var success = await processingService.ProcessFitFileAsync(fileContent, fileName);
            
            if (success)
            {
                _logger.LogInformation("Successfully processed FIT file: {fileName}", fileName);
            }
            else
            {
                _logger.LogWarning("Failed to process FIT file: {fileName}", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FIT file: {fileName}", fileName);
        }
    }

    public override void Dispose()
    {
        _semaphore.Dispose();
        base.Dispose();
    }
}