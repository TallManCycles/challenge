using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class GarminOAuthToken
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public string RequestToken { get; set; } = string.Empty;
    
    [Required]
    public string RequestTokenSecret { get; set; } = string.Empty;
    
    public string? AccessToken { get; set; }
    
    public string? AccessTokenSecret { get; set; }
    
    [Required]
    public string State { get; set; } = string.Empty;
    
    public bool IsAuthorized { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public string? OAuthVerifier { get; set; }

    public User User { get; set; } = null!;
}