using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class GarminWebhookPayload
{
    public int Id { get; set; }
    
    [Required]
    public GarminWebhookType WebhookType { get; set; }
    
    [Required]
    [Column(TypeName = "jsonb")]
    public string RawPayload { get; set; } = string.Empty;
    
    [Required]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsProcessed { get; set; } = false;
    
    public DateTime? ProcessedAt { get; set; }
    
    public string? ProcessingError { get; set; }
    
    public int? RetryCount { get; set; } = 0;
    
    public DateTime? NextRetryAt { get; set; }
}

public enum GarminWebhookType
{
    Activities,
    ActivityDetails,
    ActivityFiles,
    ManuallyUpdatedActivities,
    MoveIQActivities
}