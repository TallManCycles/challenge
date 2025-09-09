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

        // Automatically add creator as participant with calculated initial totals
        var initialTotals = await CalculateInitialTotalsForUser(currentUserId, challenge);

        await _context.Database.BeginTransactionAsync();
        
        var creatorParticipant = new ChallengeParticipant
        {
            ChallengeId = challenge.Id,
            UserId = currentUserId,
            JoinedAt = DateTime.UtcNow,
            CurrentTotal = initialTotals.CurrentTotal,
            CurrentDistance = initialTotals.CurrentDistance,
            CurrentElevation = initialTotals.CurrentElevation,
            CurrentTime = initialTotals.CurrentTime,
            LastActivityDate = initialTotals.LastActivityDate
        };

        _context.ChallengeParticipants.Add(creatorParticipant);
        await _context.SaveChangesAsync();
        
        await _context.Database.CommitTransactionAsync();


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
            ParticipantCount = 1,
            IsUserParticipating = true
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

        // Calculate initial current totals based on existing activities
        var initialTotals = await CalculateInitialTotalsForUser(currentUserId, challenge);

        var participant = new ChallengeParticipant
        {
            ChallengeId = id,
            UserId = currentUserId,
            JoinedAt = DateTime.UtcNow,
            CurrentTotal = initialTotals.CurrentTotal,
            CurrentDistance = initialTotals.CurrentDistance,
            CurrentElevation = initialTotals.CurrentElevation,
            CurrentTime = initialTotals.CurrentTime,
            LastActivityDate = initialTotals.LastActivityDate
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
                       a.ActivityDate <= challenge.EndDate
                       && a.MovingTime > 0)
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
        
        // Get all participants
        var participants = await _context.ChallengeParticipants
            .Include(cp => cp.User)
            .Where(cp => cp.ChallengeId == id)
            .ToListAsync();

        if (!participants.Any())
        {
            return Ok(new List<ChallengeLeaderboardResponse>());
        }

        var participantUserIds = participants.Select(p => p.UserId).ToList();

        // Get all activities for participants within the challenge date range
        var activities = await _context.Activities
            .Where(a => participantUserIds.Contains(a.UserId) && 
                       a.ActivityDate >= challenge.StartDate && 
                       a.ActivityDate <= challenge.EndDate)
            .ToListAsync();

        // Calculate current totals for each participant based on actual activities
        var leaderboardEntries = new List<ChallengeLeaderboardResponse>();

        foreach (var participant in participants)
        {
            var userActivities = activities.Where(a => a.UserId == participant.UserId).ToList();
            decimal currentTotal = 0;
            DateTime? lastActivityDate = null;

            foreach (var activity in userActivities)
            {
                switch (challenge.ChallengeType)
                {
                    case ChallengeType.Distance:
                        currentTotal += (decimal)activity.DistanceKm;
                        break;
                    case ChallengeType.Elevation:
                        currentTotal += (decimal)activity.ElevationGainM;
                        break;
                    case ChallengeType.Time:
                        currentTotal += activity.DurationSeconds / 3600m; // Convert seconds to hours
                        break;
                }

                if (lastActivityDate == null || activity.ActivityDate > lastActivityDate)
                {
                    lastActivityDate = activity.ActivityDate;
                }
            }

            leaderboardEntries.Add(new ChallengeLeaderboardResponse
            {
                Position = 0, // Will be set below
                UserId = participant.UserId,
                Username = participant.User.Username,
                FullName = participant.User.FullName,
                CurrentTotal = currentTotal,
                IsCurrentUser = participant.UserId == currentUserId,
                LastActivityDate = lastActivityDate?.ToUniversalTime()
            });
        }

        // Order by current total and set positions
        var orderedParticipants = leaderboardEntries.OrderByDescending(cp => cp.CurrentTotal).ToList();

        // Set positions after ordering
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

    private async Task<(decimal CurrentTotal, double CurrentDistance, double CurrentElevation, int CurrentTime, DateTime? LastActivityDate)> CalculateInitialTotalsForUser(int userId, Challenge challenge)
    {
        // Get all activities for the user within the challenge date range
        var activities = await _context.Activities
            .Where(a => a.UserId == userId && 
                       a.ActivityDate >= challenge.StartDate && 
                       a.ActivityDate <= challenge.EndDate)
            .ToListAsync();

        // Get existing Garmin activity IDs to avoid duplicates
        var existingGarminIds = activities
            .Where(a => !string.IsNullOrEmpty(a.GarminActivityId))
            .Select(a => a.GarminActivityId!)
            .ToHashSet();

        // Get processed Garmin activities that might not be in Activities table yet
        var allGarminActivities = await _context.GarminActivities
            .Where(ga => ga.UserId == userId && 
                        ga.IsProcessed && 
                        ga.StartTime >= challenge.StartDate && 
                        ga.StartTime <= challenge.EndDate)
            .ToListAsync();

        // Filter out Garmin activities that already exist in Activities table
        var garminActivities = allGarminActivities
            .Where(ga => !existingGarminIds.Contains(ga.ActivityId ?? "") && 
                        !existingGarminIds.Contains(ga.SummaryId))
            .ToList();

        double totalDistance = 0;
        double totalElevation = 0;
        int totalTimeMinutes = 0;
        DateTime? lastActivityDate = null;

        // Process regular activities
        foreach (var activity in activities)
        {
            totalDistance += activity.DistanceKm;
            totalElevation += activity.ElevationGainM;
            totalTimeMinutes += activity.DurationSeconds / 60; // Convert seconds to minutes

            if (lastActivityDate == null || activity.ActivityDate > lastActivityDate)
            {
                lastActivityDate = activity.ActivityDate;
            }
        }

        // Process Garmin activities that aren't yet in Activities table
        foreach (var garminActivity in garminActivities)
        {
            if (IsCyclingActivity(garminActivity.ActivityType))
            {
                if (garminActivity.DistanceInMeters.HasValue)
                {
                    totalDistance += garminActivity.DistanceInMeters.Value / 1000.0; // Convert meters to km
                }

                if (garminActivity.TotalElevationGainInMeters.HasValue)
                {
                    totalElevation += garminActivity.TotalElevationGainInMeters.Value;
                }

                totalTimeMinutes += garminActivity.DurationInSeconds / 60; // Convert seconds to minutes

                if (lastActivityDate == null || garminActivity.StartTime > lastActivityDate)
                {
                    lastActivityDate = garminActivity.StartTime;
                }
            }
        }

        // Calculate CurrentTotal based on challenge type
        decimal currentTotal = challenge.ChallengeType switch
        {
            ChallengeType.Distance => (decimal)totalDistance,
            ChallengeType.Elevation => (decimal)totalElevation,
            ChallengeType.Time => totalTimeMinutes / 60m, // Convert minutes to hours for display
            _ => 0
        };

        await _logger.LogInfoAsync($"Calculated initial totals for user {userId} in challenge {challenge.Id}: Distance={totalDistance:F2}km, Elevation={totalElevation:F0}m, Time={totalTimeMinutes}min, CurrentTotal={currentTotal}");

        return (currentTotal, totalDistance, totalElevation, totalTimeMinutes, lastActivityDate);
    }

    private bool IsCyclingActivity(GarminActivityType activityType)
    {
        return activityType switch
        {
            GarminActivityType.CYCLING => true,
            GarminActivityType.BMX => true,
            GarminActivityType.CYCLOCROSS => true,
            GarminActivityType.DOWNHILL_BIKING => true,
            GarminActivityType.E_BIKE_FITNESS => true,
            GarminActivityType.E_BIKE_MOUNTAIN => true,
            GarminActivityType.E_ENDURO_MTB => true,
            GarminActivityType.ENDURO_MTB => true,
            GarminActivityType.GRAVEL_CYCLING => true,
            GarminActivityType.INDOOR_CYCLING => true,
            GarminActivityType.MOUNTAIN_BIKING => true,
            GarminActivityType.RECUMBENT_CYCLING => true,
            GarminActivityType.ROAD_BIKING => true,
            GarminActivityType.TRACK_CYCLING => true,
            GarminActivityType.VIRTUAL_RIDE => true,
            GarminActivityType.HANDCYCLING => true,
            GarminActivityType.INDOOR_HANDCYCLING => true,
            _ => false
        };
    }
}