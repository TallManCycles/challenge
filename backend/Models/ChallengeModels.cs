using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class CreateChallengeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public ChallengeType ChallengeType { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
}

public class UpdateChallengeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public ChallengeType ChallengeType { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class ChallengeResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public ChallengeType ChallengeType { get; set; }
    public string ChallengeTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ParticipantCount { get; set; }
    public bool IsUserParticipating { get; set; } = false;
}

public class ChallengeParticipantResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public decimal CurrentTotal { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class ChallengeDetailsResponse : ChallengeResponse
{
    public List<ChallengeParticipantResponse> Participants { get; set; } = new();
}

public class JoinChallengeRequest
{
    // This could be empty or contain additional data if needed
}

public class ChallengeActivityResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal ElevationGain { get; set; }
    public int MovingTime { get; set; }
    public DateTime ActivityDate { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}

public class ChallengeLeaderboardResponse
{
    public int Position { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public decimal CurrentTotal { get; set; }
    public bool IsCurrentUser { get; set; }
    public DateTime? LastActivityDate { get; set; }
}

public class ChallengeDailyProgressResponse
{
    public int ChallengeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ChallengeType ChallengeType { get; set; }
    public string ChallengeTypeName { get; set; } = string.Empty;
    public List<ParticipantDailyProgress> Participants { get; set; } = new();
}

public class ParticipantDailyProgress
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsCurrentUser { get; set; }
    public List<DailyProgressEntry> DailyProgress { get; set; } = new();
}

public class DailyProgressEntry
{
    public DateTime Date { get; set; }
    public decimal DayValue { get; set; }
    public decimal CumulativeValue { get; set; }
}