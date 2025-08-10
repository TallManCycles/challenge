namespace backend.Models;

public class FitFileData
{
    public string FileName { get; set; } = string.Empty;
    public string? ZwiftUserId { get; set; }
    public DateTime ActivityStartTime { get; set; }
    public DateTime ActivityEndTime { get; set; }
    public double TotalDistanceMeters { get; set; }
    public double TotalElevationGainMeters { get; set; }
    public TimeSpan TotalTime { get; set; }
    public string ActivityType { get; set; } = "cycling"; // Default to cycling for Zwift
    public double AverageSpeed { get; set; }
    public double MaxSpeed { get; set; }
    public int? AverageHeartRate { get; set; }
    public int? MaxHeartRate { get; set; }
    public int? AveragePower { get; set; }
    public int? MaxPower { get; set; }
    public double? AverageCadence { get; set; }
    public string? WorkoutName { get; set; }
}