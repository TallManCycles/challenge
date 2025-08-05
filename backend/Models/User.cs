using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    public string? FullName { get; set; }
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? ResetToken { get; set; }
    
    public DateTime? ResetTokenExpiry { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string? GarminUserId { get; set; }
    
    public string? GarminAccessToken { get; set; }
    
    public string? GarminRefreshToken { get; set; }
    
    public DateTime? GarminConnectedAt { get; set; }

    public ICollection<Challenge> CreatedChallenges { get; set; } = new List<Challenge>();
    
    public ICollection<ChallengeParticipant> ChallengeParticipations { get; set; } = new List<ChallengeParticipant>();
    
    public bool EmailNotificationsEnabled { get; set; } = true;

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}