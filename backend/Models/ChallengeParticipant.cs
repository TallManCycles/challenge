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
    
    public decimal CurrentTotal { get; set; } = 0;
    
    public DateTime? LastActivityDate { get; set; }
}