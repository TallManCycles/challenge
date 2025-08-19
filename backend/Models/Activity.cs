using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Activity
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public string? GarminActivityId { get; set; }
    
    public string? ExternalId { get; set; } // For FIT files and other sources
    
    [Required]
    public string ActivityName { get; set; } = string.Empty;
    
    public string ActivityType { get; set; } = "cycling";
    
    public string Source { get; set; } = "Manual"; // Garmin, Zwift, Manual
    
    // Core metrics
    public double DistanceKm { get; set; }
    
    public double ElevationGainM { get; set; }
    
    public int DurationSeconds { get; set; }
    
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
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public decimal Distance 
    { 
        get => (decimal)DistanceKm; 
        set => DistanceKm = (double)value; 
    }
    
    public decimal ElevationGain 
    { 
        get => (decimal)ElevationGainM; 
        set => ElevationGainM = (double)value; 
    }
    
    public int MovingTime 
    { 
        get => DurationSeconds; 
        set => DurationSeconds = value; 
    }
}