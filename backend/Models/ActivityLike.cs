using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ActivityLike
{
    public int Id { get; set; }
    
    [Required]
    public int ActivityId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
    public User User { get; set; } = null!;
}