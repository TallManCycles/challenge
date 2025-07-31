using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Activity
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    [Required]
    public string GarminActivityId { get; set; } = string.Empty;
    
    [Required]
    public string ActivityName { get; set; } = string.Empty;
    
    public decimal Distance { get; set; }
    
    public decimal ElevationGain { get; set; }
    
    public int MovingTime { get; set; }
    
    [Required]
    public DateTime ActivityDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}