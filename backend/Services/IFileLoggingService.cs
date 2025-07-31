namespace backend.Services;

public interface IFileLoggingService
{
    Task LogAsync(string level, string message, string? category = null, Exception? exception = null);
    Task LogInfoAsync(string message, string? category = null);
    Task LogWarningAsync(string message, string? category = null);
    Task LogErrorAsync(string message, Exception? exception = null, string? category = null);
    Task LogDebugAsync(string message, string? category = null);
}