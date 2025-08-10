using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FitFileMonitorService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly FitFileMonitorOptions _options;
    private readonly HashSet<string> _processedFiles = new();

    public Worker(ILogger<Worker> logger, HttpClient httpClient, IOptions<FitFileMonitorOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fit File Monitor Service starting...");
        
        // Load previously processed files
        await LoadProcessedFilesAsync();
        
        // Expand the monitor path to replace {username} with actual username
        var expandedPath = _options.MonitorPath.Replace("{username}", Environment.UserName);
        _logger.LogInformation("Monitoring directory: {path}", expandedPath);
        _logger.LogInformation("Check interval: {minutes} minutes", _options.CheckIntervalMinutes);
        
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecuteAsync started - beginning monitoring loop");
        
        // Do an immediate check when service starts
        try
        {
            _logger.LogInformation("Performing initial check for .fit files");
            await CheckForNewFitFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial .fit files check");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Waiting {minutes} minutes until next check...", _options.CheckIntervalMinutes);
                await Task.Delay(TimeSpan.FromMinutes(_options.CheckIntervalMinutes), stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Starting scheduled check for .fit files");
                    await CheckForNewFitFilesAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for .fit files");
            }
        }
        
        _logger.LogInformation("ExecuteAsync ending - monitoring loop stopped");
    }

    private async Task CheckForNewFitFilesAsync()
    {
        _logger.LogInformation("Checking for new .fit files...");
        var monitorPath = _options.MonitorPath.Replace("{username}", Environment.UserName);
        
        if (!Directory.Exists(monitorPath))
        {
            _logger.LogWarning("Monitor directory does not exist: {path}", monitorPath);
            return;
        }

        var fitFiles = Directory.GetFiles(monitorPath, "*.fit", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found {count} .fit files in directory {path}", fitFiles.Length, monitorPath);

        if (fitFiles.Length == 0)
        {
            _logger.LogInformation("No .fit files found in directory");
            return;
        }

        foreach (var filePath in fitFiles)
        {
            var fileName = Path.GetFileName(filePath);
            
            if (_processedFiles.Contains(fileName))
            {
                continue; // Skip already processed files
            }

            try
            {
                await UploadFitFileAsync(filePath);
                _processedFiles.Add(fileName);
                await SaveProcessedFilesAsync();
                _logger.LogInformation("Successfully processed and uploaded: {fileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {fileName}", fileName);
            }
        }
    }

    private async Task UploadFitFileAsync(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        // Add secret key as header
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Secret", _options.ApiSecretKey);

        var response = await _httpClient.PostAsync($"{_options.ApiBaseUrl}/api/fitfiles/upload", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {errorContent}");
        }
    }

    private async Task LoadProcessedFilesAsync()
    {
        try
        {
            var trackingPath = _options.ProcessedFilesTrackingPath;
            var directory = Path.GetDirectoryName(trackingPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(trackingPath))
            {
                var lines = await File.ReadAllLinesAsync(trackingPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _processedFiles.Add(line.Trim());
                    }
                }
                _logger.LogInformation("Loaded {count} previously processed files", _processedFiles.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load processed files tracking");
        }
    }

    private async Task SaveProcessedFilesAsync()
    {
        try
        {
            var trackingPath = _options.ProcessedFilesTrackingPath;
            await File.WriteAllLinesAsync(trackingPath, _processedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save processed files tracking");
        }
    }
}
