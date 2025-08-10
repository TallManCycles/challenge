using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IFileLoggingService _logger;
    private readonly IFitFileReprocessingService _fitFileReprocessingService;

    public AuthController(ApplicationDbContext context, IConfiguration configuration, IFileLoggingService logger, IFitFileReprocessingService fitFileReprocessingService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _fitFileReprocessingService = fitFileReprocessingService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                await _logger.LogWarningAsync($"Registration attempt with existing email: {request.Email}", "Auth");
                return BadRequest(new { message = "Invalid registration details" });
            }

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                await _logger.LogWarningAsync($"Registration attempt with existing username: {request.Username}", "Auth");
                return BadRequest(new { message = "Invalid registration details" });
            }

        // Hash password
        var passwordHash = HashPassword(request.Password);

        // Create user
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _logger.LogInfoAsync($"New user registered: {user.Username}", "Auth");

            // Generate JWT token
            var token = GenerateJwtToken(user);

            var response = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                Token = token,
                TokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Registration failed", ex, "Auth");
            return BadRequest(new { message = "Registration failed" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            // Always verify password even if user doesn't exist to prevent timing attacks
            var passwordValid = user != null && VerifyPassword(request.Password, user.PasswordHash);
            
            if (!passwordValid)
            {
                await _logger.LogWarningAsync($"Failed login attempt for email: {request.Email}", "Auth");
                return BadRequest(new { message = "Invalid credentials" });
            }

            // Upgrade legacy SHA256 hashes to BCrypt on successful login
            if (user != null && !user.PasswordHash.StartsWith("$2"))
            {
                user.PasswordHash = HashPassword(request.Password);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _logger.LogInfoAsync($"Password hash upgraded for user: {user.Username}", "Auth");
            }

            await _logger.LogInfoAsync($"Successful login for user: {user!.Username}", "Auth");

            // Generate JWT token
            var token = GenerateJwtToken(user);

            var response = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                Token = token,
                TokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Login failed", ex, "Auth");
            return BadRequest(new { message = "Invalid credentials" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return Ok(new { message = "If an account with this email exists, a password reset link has been sent." });

        // Generate reset token
        var resetToken = GenerateResetToken();
        user.ResetToken = resetToken;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _logger.LogInfoAsync($"Password reset requested for user: {user.Username}", "Auth");

        // In a real application, you would send an email here
        // Never expose the reset token in the API response
        return Ok(new { message = "If an account with this email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.ResetToken == request.ResetToken && 
            u.ResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return BadRequest(new { message = "Invalid or expired reset token" });

        // Hash new password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _logger.LogInfoAsync($"Password reset completed for user: {user.Username}", "Auth");

        return Ok(new { message = "Password reset successfully" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get current user from JWT token
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            await _logger.LogWarningAsync($"Failed password change attempt for user: {user.Username}", "Auth");
            return BadRequest(new { message = "Invalid credentials" });
        }

        // Hash new password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _logger.LogInfoAsync($"Password changed for user: {user.Username}", "Auth");

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            username = user.Username,
            fullName = user.FullName,
            createdAt = user.CreatedAt,
            garminConnected = !string.IsNullOrEmpty(user.GarminUserId),
            emailNotificationsEnabled = user.EmailNotificationsEnabled,
            zwiftUserId = user.ZwiftUserId
        });
    }

    [HttpPatch("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get current user from JWT token
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        // Check if email is already taken by another user
        if (user.Email != request.Email && await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email is already in use by another account" });

        // Check if ZwiftUserId is already taken by another user
        if (!string.IsNullOrEmpty(request.ZwiftUserId) && 
            user.ZwiftUserId != request.ZwiftUserId && 
            await _context.Users.AnyAsync(u => u.ZwiftUserId == request.ZwiftUserId))
        {
            return BadRequest(new { message = "Zwift User ID is already in use by another account" });
        }

        var wasZwiftUserIdEmpty = string.IsNullOrEmpty(user.ZwiftUserId);
        
        // Update user profile
        user.Email = request.Email;
        user.FullName = request.FullName;
        if (request.EmailNotificationsEnabled.HasValue)
        {
            user.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
        }
        user.ZwiftUserId = request.ZwiftUserId;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _logger.LogInfoAsync($"Profile updated for user: {user.Username} - ZwiftId: {request.ZwiftUserId}", "Auth");

        // If user had no ZwiftUserId before and now does, reprocess their FIT files
        int reprocessedCount = 0;
        if (wasZwiftUserIdEmpty && !string.IsNullOrEmpty(request.ZwiftUserId))
        {
            try
            {
                reprocessedCount = await _fitFileReprocessingService.ReprocessUnprocessedFitFilesAsync(userId);
                if (reprocessedCount > 0)
                {
                    await _logger.LogInfoAsync($"Reprocessed {reprocessedCount} fit files for user {user.Username} after ZwiftUserId update", "Auth");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Failed to reprocess fit files after ZwiftUserId update", ex, "Auth");
            }
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            username = user.Username,
            fullName = user.FullName,
            emailNotificationsEnabled = user.EmailNotificationsEnabled,
            zwiftUserId = user.ZwiftUserId,
            message = "Profile updated successfully",
            reprocessedFitFiles = reprocessedCount
        });
    }


    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    private bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Try BCrypt first (new hashes)
            if (hash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            
            // Fallback to SHA256 for existing users (legacy hashes)
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + GetLegacySalt()));
            var legacyHash = Convert.ToBase64String(hashedBytes);
            return legacyHash == hash;
        }
        catch
        {
            return false;
        }
    }

    private string GetLegacySalt()
    {
        return _configuration["Auth:Salt"] ?? "DefaultSalt123!";
    }

    private string GenerateResetToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Auth:JwtKey"] ?? "DefaultJwtKey123456789012345678901234567890";
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}