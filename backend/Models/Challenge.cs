using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Challenge
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public int CreatedById { get; set; }
    
    public User CreatedBy { get; set; } = null!;
    
    [Required]
    public ChallengeType ChallengeType { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Target values based on challenge type
    public double? TargetDistance { get; set; } // in kilometers
    
    public double? TargetElevation { get; set; } // in meters
    
    public int? TargetTime { get; set; } // in minutes

    public ICollection<ChallengeParticipant> Participants { get; set; } = new List<ChallengeParticipant>();
}