using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Security.Claims;
using backend.Controllers;
using backend.Data;
using backend.Models;
using backend.Tests.Helpers;

namespace backend.Tests.Controllers;

[TestFixture]
public class ChallengeControllerTests
{
    private ApplicationDbContext _context = null!;
    private ChallengeController _controller = null!;
    private User _testUser = null!;
    private User _otherUser = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _controller = new ChallengeController(_context);

        _testUser = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _otherUser = new User
        {
            Username = "otheruser",
            Email = "other@example.com",
            PasswordHash = "hash",
            FullName = "Other User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(_testUser, _otherUser);
        await _context.SaveChangesAsync();

        SetupAuthenticatedUser(_testUser.Id, _testUser.Email, _testUser.Username);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetChallenges_ReturnsActiveChallenges()
    {
        var challenge1 = new Challenge
        {
            Title = "Test Challenge 1",
            Description = "Description 1",
            CreatedById = _testUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var challenge2 = new Challenge
        {
            Title = "Inactive Challenge",
            Description = "Description 2",
            CreatedById = _testUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.AddRange(challenge1, challenge2);
        await _context.SaveChangesAsync();

        var result = await _controller.GetChallenges();
        
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var challenges = (IEnumerable<ChallengeResponse>)okResult.Value!;
        Assert.That(challenges.Count(), Is.EqualTo(1));
        Assert.That(challenges.First().Title, Is.EqualTo("Test Challenge 1"));
        Assert.That(challenges.First().IsActive, Is.True);
    }

    [Test]
    public async Task GetChallenge_ExistingId_ReturnsChallenge()
    {
        var challenge = new Challenge
        {
            Title = "Test Challenge",
            Description = "Description",
            CreatedById = _testUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var result = await _controller.GetChallenge(challenge.Id);
        
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var challengeResponse = (ChallengeDetailsResponse)okResult.Value!;
        Assert.That(challengeResponse.Title, Is.EqualTo("Test Challenge"));
        Assert.That(challengeResponse.Participants, Is.Not.Null);
    }

    [Test]
    public async Task GetChallenge_NonExistentId_ReturnsNotFound()
    {
        var result = await _controller.GetChallenge(999);
        
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateChallenge_ValidRequest_CreatesChallenge()
    {
        var request = new CreateChallengeRequest
        {
            Title = "New Challenge",
            Description = "New Description",
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(8)
        };

        var result = await _controller.CreateChallenge(request);
        
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        var challengeResponse = (ChallengeResponse)createdResult.Value!;
        Assert.That(challengeResponse.Title, Is.EqualTo("New Challenge"));
        Assert.That(challengeResponse.CreatedById, Is.EqualTo(_testUser.Id));
        Assert.That(challengeResponse.CreatedByUsername, Is.EqualTo(_testUser.Username));
    }

    [Test]
    public async Task CreateChallenge_InvalidDates_ReturnsBadRequest()
    {
        var request = new CreateChallengeRequest
        {
            Title = "Invalid Challenge",
            Description = "Description",
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(8),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var result = await _controller.CreateChallenge(request);
        
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateChallenge_OwnChallenge_UpdatesChallenge()
    {
        var challenge = new Challenge
        {
            Title = "Original Title",
            Description = "Original Description",
            CreatedById = _testUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var request = new UpdateChallengeRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            ChallengeType = ChallengeType.Elevation,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(8),
            IsActive = false
        };

        var result = await _controller.UpdateChallenge(challenge.Id, request);
        
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var challengeResponse = (ChallengeResponse)okResult.Value!;
        Assert.That(challengeResponse.Title, Is.EqualTo("Updated Title"));
        Assert.That(challengeResponse.ChallengeType, Is.EqualTo(ChallengeType.Elevation));
        Assert.That(challengeResponse.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateChallenge_OtherUsersChallenge_ReturnsForbidden()
    {
        var challenge = new Challenge
        {
            Title = "Other's Challenge",
            Description = "Description",
            CreatedById = _otherUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var request = new UpdateChallengeRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(8)
        };

        var result = await _controller.UpdateChallenge(challenge.Id, request);
        
        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteChallenge_OwnChallenge_DeletesChallenge()
    {
        var challenge = new Challenge
        {
            Title = "To Delete",
            Description = "Description",
            CreatedById = _testUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteChallenge(challenge.Id);
        
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        var deletedChallenge = await _context.Challenges.FindAsync(challenge.Id);
        Assert.That(deletedChallenge, Is.Null);
    }

    [Test]
    public async Task DeleteChallenge_OtherUsersChallenge_ReturnsForbidden()
    {
        var challenge = new Challenge
        {
            Title = "Other's Challenge",
            Description = "Description",
            CreatedById = _otherUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteChallenge(challenge.Id);
        
        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task JoinChallenge_ActiveChallenge_JoinsSuccessfully()
    {
        var challenge = new Challenge
        {
            Title = "Join Test",
            Description = "Description",
            CreatedById = _otherUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var request = new JoinChallengeRequest();
        var result = await _controller.JoinChallenge(challenge.Id, request);
        
        Assert.That(result, Is.InstanceOf<OkObjectResult>());

        var participant = await _context.ChallengeParticipants
            .FirstOrDefaultAsync(cp => cp.ChallengeId == challenge.Id && cp.UserId == _testUser.Id);
        Assert.That(participant, Is.Not.Null);
    }

    [Test]
    public async Task JoinChallenge_InactiveChallenge_ReturnsBadRequest()
    {
        var challenge = new Challenge
        {
            Title = "Inactive Challenge",
            Description = "Description",
            CreatedById = _otherUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var request = new JoinChallengeRequest();
        var result = await _controller.JoinChallenge(challenge.Id, request);
        
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LeaveChallenge_ParticipatingChallenge_LeavesSuccessfully()
    {
        var challenge = new Challenge
        {
            Title = "Leave Test",
            Description = "Description",
            CreatedById = _otherUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var participant = new ChallengeParticipant
        {
            ChallengeId = challenge.Id,
            UserId = _testUser.Id,
            JoinedAt = DateTime.UtcNow,
            CurrentTotal = 10
        };

        _context.ChallengeParticipants.Add(participant);
        await _context.SaveChangesAsync();

        var result = await _controller.LeaveChallenge(challenge.Id);
        
        Assert.That(result, Is.InstanceOf<OkObjectResult>());

        var leftParticipant = await _context.ChallengeParticipants
            .FirstOrDefaultAsync(cp => cp.ChallengeId == challenge.Id && cp.UserId == _testUser.Id);
        Assert.That(leftParticipant, Is.Null);
    }

    [Test]
    public async Task LeaveChallenge_NotParticipating_ReturnsNotFound()
    {
        var challenge = new Challenge
        {
            Title = "Not Participating",
            Description = "Description",
            CreatedById = _otherUser.Id,
            ChallengeType = ChallengeType.Distance,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        var result = await _controller.LeaveChallenge(challenge.Id);
        
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    private void SetupAuthenticatedUser(int userId, string email, string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, username)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}