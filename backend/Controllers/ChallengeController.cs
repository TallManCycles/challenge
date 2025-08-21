using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChallengeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileLoggingService _logger;

    public ChallengeController(ApplicationDbContext context, IFileLoggingService logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChallengeResponse>>> GetChallenges()
    {
        var currentUserId = GetCurrentUserId();
        
        var challenges = await _context.Challenges
            .Include(c => c.CreatedBy)
            .Include(c => c.Participants)
            .Where(c => c.IsActive)
            .Select(c => new ChallengeResponse
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                CreatedById = c.CreatedById,
                CreatedByUsername = c.CreatedBy.Username,
                ChallengeType = c.ChallengeType,
                ChallengeTypeName = c.ChallengeType.ToString(),
                StartDate = c.StartDate.ToUniversalTime(),
                EndDate = c.EndDate.ToUniversalTime(),
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt.ToUniversalTime(),
                UpdatedAt = c.UpdatedAt.ToUniversalTime(),
                ParticipantCount = c.Participants.Count,
                IsUserParticipating = c.Participants.Any(p => p.UserId == currentUserId)
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(challenges);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChallengeDetailsResponse>> GetChallenge(int id)
    {
        var currentUserId = GetCurrentUserId();

        var challenge = await _context.Challenges
            .Include(c => c.CreatedBy)
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.Id == id)
            .Select(c => new ChallengeDetailsResponse
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                CreatedById = c.CreatedById,
                CreatedByUsername = c.CreatedBy.Username,
                ChallengeType = c.ChallengeType,
                ChallengeTypeName = c.ChallengeType.ToString(),
                StartDate = c.StartDate.ToUniversalTime(),
                EndDate = c.EndDate.ToUniversalTime(),
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt.ToUniversalTime(),
                UpdatedAt = c.UpdatedAt.ToUniversalTime(),
                ParticipantCount = c.Participants.Count,
                IsUserParticipating = c.Participants.Any(p => p.UserId == currentUserId),
                Participants = c.Participants.Select(p => new ChallengeParticipantResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Username = p.User.Username,
                    FullName = p.User.FullName,
                    JoinedAt = p.JoinedAt.ToUniversalTime(),
                    CurrentTotal = p.CurrentTotal,
                    LastActivityDate = p.LastActivityDate.HasValue ? p.LastActivityDate.Value.ToUniversalTime() : null,
                    IsCurrentUser = p.UserId == currentUserId
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (challenge == null)
        {
            return NotFound();
        }

        return Ok(challenge);
    }

    [HttpPost]
    public async Task<ActionResult<ChallengeResponse>> CreateChallenge(CreateChallengeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.EndDate <= request.StartDate)
        {
            return BadRequest(new { error = "End date must be after start date" });
        }

        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);
        
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var challenge = new Challenge
        {
            Title = request.Title,
            Description = request.Description,
            CreatedById = currentUserId,
            ChallengeType = request.ChallengeType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var response = new ChallengeResponse
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            CreatedById = challenge.CreatedById,
            CreatedByUsername = currentUser.Username,
            ChallengeType = challenge.ChallengeType,
            ChallengeTypeName = challenge.ChallengeType.ToString(),
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            IsActive = challenge.IsActive,
            CreatedAt = challenge.CreatedAt,
            UpdatedAt = challenge.UpdatedAt,
            ParticipantCount = 0,
            IsUserParticipating = false
        };

        return CreatedAtAction(nameof(GetChallenge), new { id = challenge.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ChallengeResponse>> UpdateChallenge(int id, UpdateChallengeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.EndDate <= request.StartDate)
        {
            return BadRequest(new { error = "End date must be after start date" });
        }

        var currentUserId = GetCurrentUserId();
        var challenge = await _context.Challenges
            .Include(c => c.CreatedBy)
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challenge == null)
        {
            return NotFound();
        }

        if (challenge.CreatedById != currentUserId)
        {
            return Forbid();
        }

        challenge.Title = request.Title;
        challenge.Description = request.Description;
        challenge.ChallengeType = request.ChallengeType;
        challenge.StartDate = request.StartDate;
        challenge.EndDate = request.EndDate;
        challenge.IsActive = request.IsActive;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = new ChallengeResponse
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            CreatedById = challenge.CreatedById,
            CreatedByUsername = challenge.CreatedBy.Username,
            ChallengeType = challenge.ChallengeType,
            ChallengeTypeName = challenge.ChallengeType.ToString(),
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            IsActive = challenge.IsActive,
            CreatedAt = challenge.CreatedAt,
            UpdatedAt = challenge.UpdatedAt,
            ParticipantCount = challenge.Participants.Count,
            IsUserParticipating = challenge.Participants.Any(p => p.UserId == currentUserId)
        };

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChallenge(int id)
    {
        var currentUserId = GetCurrentUserId();
        var challenge = await _context.Challenges.FindAsync(id);

        if (challenge == null)
        {
            return NotFound();
        }

        if (challenge.CreatedById != currentUserId)
        {
            return Forbid();
        }

        _context.Challenges.Remove(challenge);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinChallenge(int id, JoinChallengeRequest request)
    {
        var currentUserId = GetCurrentUserId();
        
        var challenge = await _context.Challenges.FindAsync(id);
        if (challenge == null)
        {
            return NotFound();
        }

        if (!challenge.IsActive)
        {
            return BadRequest(new { error = "Challenge is not active" });
        }

        if (DateTime.UtcNow > challenge.EndDate)
        {
            return BadRequest(new { error = "Challenge has already ended" });
        }

        var existingParticipant = await _context.ChallengeParticipants
            .FirstOrDefaultAsync(cp => cp.ChallengeId == id && cp.UserId == currentUserId);

        if (existingParticipant != null)
        {
            return BadRequest(new { error = "Already participating in this challenge" });
        }

        var participant = new ChallengeParticipant
        {
            ChallengeId = id,
            UserId = currentUserId,
            JoinedAt = DateTime.UtcNow,
            CurrentTotal = 0,
            LastActivityDate = null
        };

        _context.ChallengeParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully joined challenge" });
    }

    [HttpDelete("{id}/leave")]
    public async Task<IActionResult> LeaveChallenge(int id)
    {
        var currentUserId = GetCurrentUserId();
        
        var participant = await _context.ChallengeParticipants
            .FirstOrDefaultAsync(cp => cp.ChallengeId == id && cp.UserId == currentUserId);

        if (participant == null)
        {
            return NotFound(new { error = "Not participating in this challenge" });
        }

        _context.ChallengeParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Successfully left challenge" });
    }

    [HttpGet("{id}/activities")]
    public async Task<ActionResult<IEnumerable<ChallengeActivityResponse>>> GetChallengeActivities(int id, [FromQuery] int limit = 10)
    {
        var currentUserId = GetCurrentUserId();
        var challenge = await _context.Challenges.FindAsync(id);
        if (challenge == null)
        {
            return NotFound();
        }

        // Get recent activities from challenge participants with like information
        var activities = await _context.Activities
            .Include(a => a.User)
            .Where(a => a.User.ChallengeParticipations.Any(cp => cp.ChallengeId == id) && 
                       a.ActivityDate >= challenge.StartDate && 
                       a.ActivityDate <= challenge.EndDate)
            .OrderByDescending(a => a.ActivityDate)
            .Take(limit)
            .Select(a => new ChallengeActivityResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                Username = a.User.Username,
                FullName = a.User.FullName,
                ActivityName = a.ActivityName,
                Distance = a.Distance,
                ElevationGain = a.ElevationGain,
                MovingTime = a.MovingTime,
                ActivityDate = a.ActivityDate.ToUniversalTime(),
                LikeCount = _context.ActivityLikes.Count(al => al.ActivityId == a.Id),
                IsLikedByCurrentUser = _context.ActivityLikes.Any(al => al.ActivityId == a.Id && al.UserId == currentUserId)
            })
            .ToListAsync();

        return Ok(activities);
    }

    [HttpGet("{id}/leaderboard")]
    public async Task<ActionResult<IEnumerable<ChallengeLeaderboardResponse>>> GetChallengeLeaderboard(int id)
    {
        var currentUserId = GetCurrentUserId();
        
        var challenge = await _context.Challenges.FindAsync(id);
        if (challenge == null)
        {
            return NotFound();
        }
        
        // SQLite does not support expressions of type 'decimal' in ORDER BY clauses. Convert the values to a supported type, or use LINQ to Objects to order the results on the client side.

        var participants = await _context.ChallengeParticipants
            .Include(cp => cp.User)
            .Where(cp => cp.ChallengeId == id)
            .Select(cp => new ChallengeLeaderboardResponse
            {
                Position = 0, // Will be set below
                UserId = cp.UserId,
                Username = cp.User.Username,
                FullName = cp.User.FullName,
                CurrentTotal = cp.CurrentTotal,
                IsCurrentUser = cp.UserId == currentUserId,
                LastActivityDate = cp.LastActivityDate.HasValue ? cp.LastActivityDate.Value.ToUniversalTime() : null
            })
            .ToListAsync();

        var orderedParticipants = participants.OrderByDescending(cp => cp.CurrentTotal).ToList();

        // Set positions after retrieving from database
        for (int i = 0; i < orderedParticipants.Count; i++)
        {
            orderedParticipants[i].Position = i + 1;
        }

        return Ok(orderedParticipants);
    }

    [HttpGet("{id}/progress")]
    public async Task<ActionResult<ChallengeDailyProgressResponse>> GetChallengeDailyProgress(int id)
    {
        var currentUserId = GetCurrentUserId();
        
        var challenge = await _context.Challenges.FindAsync(id);
        if (challenge == null)
        {
            return NotFound();
        }

        // Get all participants
        var participants = await _context.ChallengeParticipants
            .Include(cp => cp.User)
            .Where(cp => cp.ChallengeId == id)
            .ToListAsync();

        if (!participants.Any())
        {
            return Ok(new ChallengeDailyProgressResponse
            {
                ChallengeId = id,
                StartDate = challenge.StartDate.ToUniversalTime(),
                EndDate = challenge.EndDate.ToUniversalTime(),
                Participants = new List<ParticipantDailyProgress>()
            });
        }

        var participantUserIds = participants.Select(p => p.UserId).ToList();

        // Get all activities for participants within the challenge date range
        var activities = await _context.Activities
            .Where(a => participantUserIds.Contains(a.UserId) && 
                       a.ActivityDate >= challenge.StartDate && 
                       a.ActivityDate <= challenge.EndDate)
            .OrderBy(a => a.ActivityDate)
            .ToListAsync();

        // Generate date range from challenge start to end (or today if ongoing)
        var endDate = challenge.EndDate > DateTime.UtcNow ? DateTime.UtcNow.Date : challenge.EndDate.Date;
        var dateRange = GenerateDateRange(challenge.StartDate.Date, endDate);

        // Calculate daily progress for each participant
        var participantProgressList = new List<ParticipantDailyProgress>();

        foreach (var participant in participants)
        {
            var userActivities = activities.Where(a => a.UserId == participant.UserId).ToList();
            var dailyProgress = new List<DailyProgressEntry>();
            decimal cumulativeTotal = 0;

            foreach (var date in dateRange)
            {
                var dayActivities = userActivities.Where(a => a.ActivityDate.Date == date).ToList();
                decimal dayValue = 0;

                foreach (var activity in dayActivities)
                {
                    switch (challenge.ChallengeType)
                    {
                        case ChallengeType.Distance:
                            dayValue += (decimal)activity.DistanceKm;
                            break;
                        case ChallengeType.Elevation:
                            dayValue += (decimal)activity.ElevationGainM;
                            break;
                        case ChallengeType.Time:
                            dayValue += activity.DurationSeconds / 3600m; // Convert seconds to hours
                            break;
                    }
                }

                cumulativeTotal += dayValue;
                dailyProgress.Add(new DailyProgressEntry
                {
                    Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                    DayValue = dayValue,
                    CumulativeValue = cumulativeTotal
                });
            }

            participantProgressList.Add(new ParticipantDailyProgress
            {
                UserId = participant.UserId,
                Username = participant.User.Username,
                FullName = participant.User.FullName,
                IsCurrentUser = participant.UserId == currentUserId,
                DailyProgress = dailyProgress
            });
        }

        return Ok(new ChallengeDailyProgressResponse
        {
            ChallengeId = id,
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            ChallengeType = challenge.ChallengeType,
            ChallengeTypeName = challenge.ChallengeType.ToString(),
            Participants = participantProgressList
        });
    }

    private List<DateTime> GenerateDateRange(DateTime startDate, DateTime endDate)
    {
        var dates = new List<DateTime>();
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            dates.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }
        
        return dates;
    }
}