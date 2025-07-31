using System.Text.Json;

namespace backend.Services;

public class FileLoggingService : IFileLoggingService
{
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _semaphore;

    public FileLoggingService()
    {
        _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        _semaphore = new SemaphoreSlim(1, 1);
        
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public async Task LogAsync(string level, string message, string? category = null, Exception? exception = null)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Category = category ?? "General",
            Message = message,
            Exception = exception?.ToString()
        };

        var logText = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
        var filePath = Path.Combine(_logDirectory, fileName);

        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, logText + Environment.NewLine + Environment.NewLine);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task LogInfoAsync(string message, string? category = null)
    {
        await LogAsync("INFO", message, category);
    }

    public async Task LogWarningAsync(string message, string? category = null)
    {
        await LogAsync("WARNING", message, category);
    }

    public async Task LogErrorAsync(string message, Exception? exception = null, string? category = null)
    {
        await LogAsync("ERROR", message, category, exception);
    }

    public async Task LogDebugAsync(string message, string? category = null)
    {
        await LogAsync("DEBUG", message, category);
    }
}