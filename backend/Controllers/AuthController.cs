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

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email already registered" });

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "Username already taken" });

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

        // Generate JWT token
        var token = GenerateJwtToken(user);

        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Token = token,
            TokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Find user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return BadRequest(new { message = "Invalid email or password" });

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash))
            return BadRequest(new { message = "Invalid email or password" });

        // Generate JWT token
        var token = GenerateJwtToken(user);

        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Token = token,
            TokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        return Ok(response);
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

        // In a real application, you would send an email here
        // For now, we'll just return the token (remove this in production)
        return Ok(new { message = "Password reset token generated", resetToken = resetToken });
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
            return BadRequest(new { message = "Current password is incorrect" });

        // Hash new password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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
            garminConnected = !string.IsNullOrEmpty(user.GarminUserId)
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

        // Update user profile
        user.Email = request.Email;
        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            username = user.Username,
            fullName = user.FullName,
            message = "Profile updated successfully"
        });
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + GetSalt()));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return computedHash == hash;
    }

    private string GetSalt()
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
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}