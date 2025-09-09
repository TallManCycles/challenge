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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            
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
            zwiftUserId = user.ZwiftUserId,
            profilePhotoUrl = user.ProfilePhotoUrl
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
            profilePhotoUrl = user.ProfilePhotoUrl,
            message = "Profile updated successfully",
            reprocessedFitFiles = reprocessedCount
        });
    }

    [HttpPost("profile-photo")]
    [Authorize]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile photo)
    {
        // Get current user from JWT token
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        if (photo == null || photo.Length == 0)
            return BadRequest(new { message = "No photo file provided" });

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(photo.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Only JPEG, PNG, and WebP images are allowed" });
        }

        // Validate file size (5MB max)
        const int maxSizeBytes = 5 * 1024 * 1024;
        if (photo.Length > maxSizeBytes)
        {
            return BadRequest(new { message = "File size must be less than 5MB" });
        }

        try
        {
            // Create uploads directory - prefer the persistent volume location
            var persistentUploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "profile-photos");
            var wwwrootUploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-photos");
            
            // Try persistent location first (mounted volume in production)
            string uploadsDir;
            try
            {
                Directory.CreateDirectory(persistentUploadsDir);
                uploadsDir = persistentUploadsDir;
                await _logger.LogInfoAsync($"Using persistent uploads directory: {uploadsDir}", "Auth");
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Could not use persistent uploads directory '{persistentUploadsDir}'. Error: {ex.GetType().Name}: {ex.Message}", "Auth");
                
                try
                {
                    // Fall back to wwwroot location
                    Directory.CreateDirectory(wwwrootUploadsDir);
                    uploadsDir = wwwrootUploadsDir;
                    await _logger.LogInfoAsync($"Successfully fell back to wwwroot uploads directory: {uploadsDir}", "Auth");
                }
                catch (Exception wwwEx)
                {
                    await _logger.LogErrorAsync($"Critical error: Cannot create uploads directory in either location. Persistent dir error: {ex.Message}, wwwroot dir error: {wwwEx.Message}", wwwEx, "Auth");
                    throw new InvalidOperationException("Unable to create uploads directory. Check file system permissions and disk space.", wwwEx);
                }
            }

            // Generate unique filename
            var extension = Path.GetExtension(photo.FileName).ToLower();
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Delete old profile photo if exists - check persistent location first
            if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
            {
                var oldFileName = Path.GetFileName(user.ProfilePhotoUrl);
                var oldPersistentPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "profile-photos", oldFileName);
                var oldWwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-photos", oldFileName);
                
                try
                {
                    if (System.IO.File.Exists(oldPersistentPath))
                    {
                        System.IO.File.Delete(oldPersistentPath);
                        await _logger.LogInfoAsync($"Deleted old profile photo from persistent storage: {oldFileName}", "Auth");
                    }
                    else if (System.IO.File.Exists(oldWwwrootPath))
                    {
                        System.IO.File.Delete(oldWwwrootPath);
                        await _logger.LogInfoAsync($"Deleted old profile photo from wwwroot: {oldFileName}", "Auth");
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogWarningAsync($"Failed to delete old profile photo '{oldFileName}' for user {user.Username}. Error: {ex.GetType().Name}: {ex.Message}", "Auth");
                    // Continue with upload even if old file deletion fails
                }
            }

            // Save new file
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
                await _logger.LogInfoAsync($"Successfully saved profile photo: {fileName}", "Auth");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Failed to save profile photo file '{fileName}' to '{filePath}'. Error: {ex.GetType().Name}: {ex.Message}", ex, "Auth");
                throw new InvalidOperationException($"Failed to save profile photo. Error: {ex.Message}", ex);
            }

            // Update user profile
            user.ProfilePhotoUrl = $"/uploads/profile-photos/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;
            
            try
            {
                await _context.SaveChangesAsync();
                await _logger.LogInfoAsync($"Profile photo updated for user: {user.Username}", "Auth");
            }
            catch (Exception ex)
            {
                // If database update fails, try to clean up the uploaded file
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        await _logger.LogInfoAsync($"Cleaned up uploaded file after database error: {fileName}", "Auth");
                    }
                }
                catch (Exception cleanupEx)
                {
                    await _logger.LogWarningAsync($"Failed to cleanup file after database error. File: {fileName}, Cleanup Error: {cleanupEx.Message}", "Auth");
                }
                
                await _logger.LogErrorAsync($"Failed to update user profile photo in database for user: {user.Username}. Error: {ex.GetType().Name}: {ex.Message}", ex, "Auth");
                throw new InvalidOperationException($"Failed to save profile photo metadata. Error: {ex.Message}", ex);
            }

            return Ok(new
            {
                message = "Profile photo updated successfully",
                profilePhotoUrl = user.ProfilePhotoUrl
            });
        }
        catch (InvalidOperationException ex)
        {
            // These are expected errors we've already logged
            return StatusCode(500, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            await _logger.LogErrorAsync($"Permission denied while uploading profile photo for user {userId}", ex, "Auth");
            return StatusCode(500, new { message = "Permission denied. Check file system permissions." });
        }
        catch (IOException ex)
        {
            await _logger.LogErrorAsync($"File I/O error during profile photo upload for user {userId}", ex, "Auth");
            return StatusCode(500, new { message = "File system error. Check disk space and permissions." });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Unexpected error during profile photo upload for user {userId}", ex, "Auth");
            return StatusCode(500, new { message = "An unexpected error occurred while uploading the profile photo." });
        }
    }

    [HttpDelete("profile-photo")]
    [Authorize]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        // Get current user from JWT token
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
            return BadRequest(new { message = "No profile photo to delete" });

        try
        {
            // Delete file from disk - check persistent location first
            var fileName = Path.GetFileName(user.ProfilePhotoUrl);
            var persistentPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "profile-photos", fileName);
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-photos", fileName);
            
            try
            {
                if (System.IO.File.Exists(persistentPath))
                {
                    System.IO.File.Delete(persistentPath);
                    await _logger.LogInfoAsync($"Deleted profile photo from persistent storage: {persistentPath}", "Auth");
                }
                else if (System.IO.File.Exists(wwwrootPath))
                {
                    System.IO.File.Delete(wwwrootPath);
                    await _logger.LogInfoAsync($"Deleted profile photo from wwwroot: {wwwrootPath}", "Auth");
                }
                else
                {
                    await _logger.LogWarningAsync($"Profile photo file not found in either location for user: {user.Username}. Expected locations: {persistentPath} or {wwwrootPath}", "Auth");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Failed to delete profile photo file '{fileName}' for user {user.Username}. Error: {ex.GetType().Name}: {ex.Message}", ex, "Auth");
                // Continue to update database even if file deletion fails
            }

            // Update user profile
            user.ProfilePhotoUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logger.LogInfoAsync($"Profile photo deleted for user: {user.Username}", "Auth");

            return Ok(new { message = "Profile photo deleted successfully" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Failed to delete profile photo", ex, "Auth");
            return StatusCode(500, new { message = "Failed to delete profile photo" });
        }
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
            Expires = DateTime.UtcNow.AddHours(72),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}