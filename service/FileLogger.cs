using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FitFileMonitorService;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _filePath));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePath;
    private static readonly object _lock = new object();

    public FileLogger(string categoryName, string filePath)
    {
        _categoryName = categoryName;
        _filePath = filePath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var logMessage = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logLevel_str = logLevel.ToString().ToUpper();
        
        var fullMessage = $"[{timestamp}] [{logLevel_str}] {_categoryName}: {logMessage}";
        
        if (exception != null)
        {
            fullMessage += Environment.NewLine + $"Exception: {exception}";
        }
        
        fullMessage += Environment.NewLine;

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_filePath, fullMessage);
            }
            catch
            {
                // Ignore file write errors to prevent logging loops
            }
        }
    }
}