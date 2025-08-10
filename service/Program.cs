using FitFileMonitorService;

var builder = Host.CreateApplicationBuilder(args);

// Configure the service for Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Fit File Monitor Service";
});

// Configure file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(); // Windows Event Log
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Add custom file logging
var serviceDirectory = AppDomain.CurrentDomain.BaseDirectory;
var logFilePath = Path.Combine(serviceDirectory, "log.txt");
builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));

// Configure options
builder.Services.Configure<FitFileMonitorOptions>(
    builder.Configuration.GetSection("FitFileMonitor"));

// Add HTTP client for API calls
builder.Services.AddHttpClient();

// Add the worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
