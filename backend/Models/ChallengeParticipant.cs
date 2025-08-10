using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ChallengeParticipant
{
    public int Id { get; set; }
    
    [Required]
    public int ChallengeId { get; set; }
    
    public Challenge Challenge { get; set; } = null!;
    
    [Required]
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public decimal CurrentTotal { get; set; } = 0; // Legacy field
    
    // Progress tracking fields
    public double CurrentDistance { get; set; } = 0; // in kilometers
    
    public double CurrentElevation { get; set; } = 0; // in meters
    
    public int CurrentTime { get; set; } = 0; // in minutes
    
    public bool IsCompleted { get; set; } = false;
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime? LastActivityDate { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}