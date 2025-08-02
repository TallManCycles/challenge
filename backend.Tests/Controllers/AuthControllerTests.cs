using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Security.Claims;
using backend.Controllers;
using backend.Data;
using backend.Models;
using backend.Services;
using backend.Tests.Helpers;

namespace backend.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private ApplicationDbContext _context;
    private IConfiguration _configuration;
    private AuthController _controller;
    private IFileLoggingService _logger;

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _configuration = TestDbContextFactory.CreateTestConfiguration();
        _logger = TestDbContextFactory.CreateTestLogger();
        _controller = new AuthController(_context, _configuration, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestFixture]
    public class RegisterTests : AuthControllerTests
    {
        [Test]
        public async Task Register_ValidRequest_ReturnsOkWithAuthResponse()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "password123",
                ConfirmPassword = "password123"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as AuthResponse;
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Email, Is.EqualTo("test@example.com"));
            Assert.That(response.Username, Is.EqualTo("testuser"));
            Assert.That(response.Token, Is.Not.Empty);
            Assert.That(response.UserId, Is.GreaterThan(0));

            // Verify user was created in database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.That(user, Is.Not.Null);
            Assert.That(user.PasswordHash, Is.Not.Empty);
        }

        [Test]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "test@example.com",
                Username = "existinguser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Username = "newuser",
                Password = "password123",
                ConfirmPassword = "password123"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid registration details"));
        }

        [Test]
        public async Task Register_DuplicateUsername_ReturnsBadRequest()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "existing@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = new RegisterRequest
            {
                Email = "new@example.com",
                Username = "testuser",
                Password = "password123",
                ConfirmPassword = "password123"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid registration details"));
        }

        [Test]
        public async Task Register_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "invalid-email",
                Username = "",
                Password = "123", // Too short
                ConfirmPassword = "different"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");
            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }

    [TestFixture]
    public class LoginTests : AuthControllerTests
    {
        [Test]
        public async Task Login_ValidCredentials_ReturnsOkWithAuthResponse()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = HashPassword("password123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as AuthResponse;
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Email, Is.EqualTo("test@example.com"));
            Assert.That(response.Username, Is.EqualTo("testuser"));
            Assert.That(response.Token, Is.Not.Empty);
        }

        [Test]
        public async Task Login_InvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "password123"
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid credentials"));
        }

        [Test]
        public async Task Login_InvalidPassword_ReturnsBadRequest()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = HashPassword("correctpassword"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid credentials"));
        }

        [Test]
        public async Task Login_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "invalid-email",
                Password = ""
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }

    [TestFixture]
    public class ForgotPasswordTests : AuthControllerTests
    {
        [Test]
        public async Task ForgotPassword_ValidEmail_ReturnsOkWithResetToken()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            
            // Verify reset token was set in database
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.That(updatedUser.ResetToken, Is.Not.Null);
            Assert.That(updatedUser.ResetTokenExpiry, Is.Not.Null);
            Assert.That(updatedUser.ResetTokenExpiry, Is.GreaterThan(DateTime.UtcNow));
        }

        [Test]
        public async Task ForgotPassword_NonExistentEmail_ReturnsOkWithGenericMessage()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value;
            Assert.That(response.ToString(), Does.Contain("If an account with this email exists"));
        }
    }

    [TestFixture]
    public class ResetPasswordTests : AuthControllerTests
    {
        [Test]
        public async Task ResetPassword_ValidToken_ReturnsOkAndUpdatesPassword()
        {
            // Arrange
            var resetToken = "valid-reset-token";
            var user = new User
            {
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "oldhashedpassword",
                ResetToken = resetToken,
                ResetTokenExpiry = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            
            // Verify password was updated and reset token was cleared
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.That(updatedUser.PasswordHash, Is.Not.EqualTo("oldhashedpassword"));
            Assert.That(updatedUser.ResetToken, Is.Null);
            Assert.That(updatedUser.ResetTokenExpiry, Is.Null);
        }

        [Test]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                ResetToken = "invalid-token",
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid or expired reset token"));
        }

        [Test]
        public async Task ResetPassword_ExpiredToken_ReturnsBadRequest()
        {
            // Arrange
            var resetToken = "expired-reset-token";
            var user = new User
            {
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                ResetToken = resetToken,
                ResetTokenExpiry = DateTime.UtcNow.AddHours(-1), // Expired
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid or expired reset token"));
        }
    }

    private string HashPassword(string password)
    {
        var salt = _configuration["Auth:Salt"] ?? "DefaultSalt123!";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(hashedBytes);
    }
}