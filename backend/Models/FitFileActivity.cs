using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class FitFileActivity
{
    public int Id { get; set; }
    
    public int? UserId { get; set; }
    
    public User? User { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    public string? ZwiftUserId { get; set; }
    
    [Required]
    public string ActivityName { get; set; } = string.Empty;
    
    public string ActivityType { get; set; } = "cycling";
    
    // Core metrics
    public double DistanceKm { get; set; }
    
    public double ElevationGainM { get; set; }
    
    public int DurationMinutes { get; set; }
    
    // Timing
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    [Required]
    public DateTime ActivityDate { get; set; }
    
    // Performance metrics
    public double? AverageSpeed { get; set; }
    
    public double? MaxSpeed { get; set; }
    
    public int? AverageHeartRate { get; set; }
    
    public int? MaxHeartRate { get; set; }
    
    public int? AveragePower { get; set; }
    
    public int? MaxPower { get; set; }
    
    public double? AverageCadence { get; set; }
    
    // Processing status
    [Required]
    public FitFileProcessingStatus Status { get; set; } = FitFileProcessingStatus.Unprocessed;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? LastProcessingAttempt { get; set; }
    
    public string? ProcessingError { get; set; }
    
    // Challenge processing
    public bool ChallengesProcessed { get; set; } = false;
    
    public DateTime? ChallengesProcessedAt { get; set; }
}

public enum FitFileProcessingStatus
{
    Unprocessed,        // User not found or no zwiftid
    Processed,          // Successfully processed and added to challenges
    Failed,            // Processing failed with error
    UserNotFound       // ZwiftUserId in file but no matching user
}