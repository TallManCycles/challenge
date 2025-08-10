namespace backend.Models;

public class UnifiedActivityResponse
{
    public int Id { get; set; }
    public string SummaryId { get; set; } = string.Empty;
    public string? ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? ActivityName { get; set; }
    public DateTime StartTime { get; set; }
    public int StartTimeOffsetInSeconds { get; set; }
    public int DurationInSeconds { get; set; }
    public double? DistanceInMeters { get; set; }
    public double? TotalElevationGainInMeters { get; set; }
    public double? TotalElevationLossInMeters { get; set; }
    public int? ActiveKilocalories { get; set; }
    public string? DeviceName { get; set; }
    public bool IsManual { get; set; }
    public bool IsWebUpload { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    
    // Source to distinguish between Garmin Connect and FIT file
    public string Source { get; set; } = "GarminConnect"; // "GarminConnect" or "FitFile"
    
    // FIT file specific fields
    public string? FileName { get; set; }
    public string? ZwiftUserId { get; set; }
}

public static class ActivityMapper
{
    public static UnifiedActivityResponse MapFromGarminActivity(GarminActivity activity)
    {
        return new UnifiedActivityResponse
        {
            Id = activity.Id,
            SummaryId = activity.SummaryId,
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType.ToString(),
            ActivityName = activity.ActivityName,
            StartTime = activity.StartTime,
            StartTimeOffsetInSeconds = activity.StartTimeOffsetInSeconds,
            DurationInSeconds = activity.DurationInSeconds,
            DistanceInMeters = activity.DistanceInMeters,
            TotalElevationGainInMeters = activity.TotalElevationGainInMeters,
            TotalElevationLossInMeters = activity.TotalElevationLossInMeters,
            ActiveKilocalories = activity.ActiveKilocalories,
            DeviceName = activity.DeviceName,
            IsManual = activity.IsManual,
            IsWebUpload = activity.IsWebUpload,
            ReceivedAt = activity.ReceivedAt,
            ProcessedAt = activity.ProcessedAt,
            IsProcessed = activity.IsProcessed,
            Source = "GarminConnect"
        };
    }
    
    public static UnifiedActivityResponse MapFromFitFileActivity(FitFileActivity fitActivity)
    {
        return new UnifiedActivityResponse
        {
            Id = fitActivity.Id,
            SummaryId = fitActivity.FileName, // Use filename as SummaryId for FIT files
            ActivityId = null, // FIT files don't have Garmin ActivityId
            ActivityType = fitActivity.ActivityType,
            ActivityName = fitActivity.ActivityName,
            StartTime = fitActivity.StartTime,
            StartTimeOffsetInSeconds = 0, // FIT files don't have timezone offset
            DurationInSeconds = fitActivity.DurationMinutes * 60,
            DistanceInMeters = fitActivity.DistanceKm * 1000,
            TotalElevationGainInMeters = fitActivity.ElevationGainM,
            TotalElevationLossInMeters = null, // FIT files don't track elevation loss
            ActiveKilocalories = null, // FIT files don't typically have calories
            DeviceName = "Zwift", // Assume Zwift as device for FIT files
            IsManual = false,
            IsWebUpload = false,
            ReceivedAt = fitActivity.CreatedAt,
            ProcessedAt = fitActivity.ProcessedAt,
            IsProcessed = fitActivity.Status == FitFileProcessingStatus.Processed,
            Source = "FitFile",
            FileName = fitActivity.FileName,
            ZwiftUserId = fitActivity.ZwiftUserId
        };
    }
}