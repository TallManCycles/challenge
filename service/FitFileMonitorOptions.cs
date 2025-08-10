namespace FitFileMonitorService;

public class FitFileMonitorOptions
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ApiSecretKey { get; set; } = string.Empty;
    public string MonitorPath { get; set; } = string.Empty;
    public int CheckIntervalMinutes { get; set; } = 5;
    public string ProcessedFilesTrackingPath { get; set; } = string.Empty;
}