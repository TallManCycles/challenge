using Microsoft.AspNetCore.Http;
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
public class AuthControllerProtectedEndpointsTests
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
    public class ChangePasswordTests : AuthControllerProtectedEndpointsTests
    {
        [Test]
        public async Task ChangePassword_ValidRequest_ReturnsOkAndUpdatesPassword()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = HashPassword("oldpassword"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Mock authenticated user
            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "oldpassword",
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            
            // Verify password was updated (should now be BCrypt hash)
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.That(updatedUser.PasswordHash, Is.Not.EqualTo(HashPassword("oldpassword")));
            // BCrypt hashes start with $2, verify it's now using BCrypt
            Assert.That(updatedUser.PasswordHash, Does.StartWith("$2"));
        }

        [Test]
        public async Task ChangePassword_IncorrectCurrentPassword_ReturnsBadRequest()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = HashPassword("correctpassword"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "wrongpassword",
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Invalid credentials"));
        }

        [Test]
        public async Task ChangePassword_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "oldpassword",
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task ChangePassword_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(999, "nonexistent@example.com", "nonexistent");

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "oldpassword",
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task ChangePassword_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = HashPassword("oldpassword"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "",
                NewPassword = "123", // Too short
                ConfirmNewPassword = "different"
            };

            _controller.ModelState.AddModelError("CurrentPassword", "Current password is required");
            _controller.ModelState.AddModelError("NewPassword", "Password too short");

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }

    [TestFixture]
    public class GetCurrentUserTests : AuthControllerProtectedEndpointsTests
    {
        [Test]
        public async Task GetCurrentUser_AuthenticatedUser_ReturnsOkWithUserInfo()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                GarminUserId = "garmin123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            
            // Use reflection to access anonymous object properties
            var responseType = okResult.Value?.GetType();
            var userIdProperty = responseType?.GetProperty("userId");
            var emailProperty = responseType?.GetProperty("email");
            var usernameProperty = responseType?.GetProperty("username");
            var garminConnectedProperty = responseType?.GetProperty("garminConnected");
            
            Assert.That(userIdProperty?.GetValue(okResult.Value), Is.EqualTo(1));
            Assert.That(emailProperty?.GetValue(okResult.Value), Is.EqualTo("test@example.com"));
            Assert.That(usernameProperty?.GetValue(okResult.Value), Is.EqualTo("testuser"));
            Assert.That(garminConnectedProperty?.GetValue(okResult.Value), Is.True);
        }

        [Test]
        public async Task GetCurrentUser_UserWithoutGarmin_ReturnsOkWithGarminConnectedFalse()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                GarminUserId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            
            var responseType = okResult.Value?.GetType();
            var garminConnectedProperty = responseType?.GetProperty("garminConnected");
            Assert.That(garminConnectedProperty?.GetValue(okResult.Value), Is.False);
        }

        [Test]
        public async Task GetCurrentUser_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(999, "nonexistent@example.com", "nonexistent");

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetCurrentUser_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            SetupAuthenticatedUserWithInvalidId();

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }
    }

    [TestFixture]
    public class UpdateProfileTests : AuthControllerProtectedEndpointsTests
    {
        [Test]
        public async Task UpdateProfile_ValidRequest_ReturnsOkAndUpdatesProfile()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "old@example.com",
                Username = "testuser",
                FullName = "Old Name",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            var request = new UpdateProfileRequest
            {
                Email = "new@example.com",
                FullName = "New Full Name"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());

            // Verify profile was updated
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.That(updatedUser.Email, Is.EqualTo("new@example.com"));
            Assert.That(updatedUser.FullName, Is.EqualTo("New Full Name"));
            Assert.That(updatedUser.UpdatedAt, Is.GreaterThanOrEqualTo(user.UpdatedAt));
        }

        [Test]
        public async Task UpdateProfile_EmailAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            var existingUser = new User
            {
                Id = 2,
                Email = "existing@example.com",
                Username = "existinguser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);

            var currentUser = new User
            {
                Id = 1,
                Email = "current@example.com",
                Username = "currentuser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(currentUser);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(currentUser.Id, currentUser.Email, currentUser.Username);

            var request = new UpdateProfileRequest
            {
                Email = "existing@example.com", // Email already taken by another user
                FullName = "New Name"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = badRequestResult.Value;
            Assert.That(errorResponse.ToString(), Does.Contain("Email is already in use"));
        }

        [Test]
        public async Task UpdateProfile_SameEmailAsCurrent_ReturnsOkAndUpdatesProfile()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "current@example.com",
                Username = "testuser",
                FullName = "Old Name",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            var request = new UpdateProfileRequest
            {
                Email = "current@example.com", // Same email as current user
                FullName = "New Full Name"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());

            // Verify profile was updated
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.That(updatedUser.FullName, Is.EqualTo("New Full Name"));
        }

        [Test]
        public async Task UpdateProfile_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new UpdateProfileRequest
            {
                Email = "new@example.com",
                FullName = "New Name"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task UpdateProfile_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(999, "nonexistent@example.com", "nonexistent");

            var request = new UpdateProfileRequest
            {
                Email = "new@example.com",
                FullName = "New Name"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task UpdateProfile_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "current@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetupAuthenticatedUser(user.Id, user.Email, user.Username);

            var request = new UpdateProfileRequest
            {
                Email = "invalid-email", // Invalid email format
                FullName = "New Name"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
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

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private void SetupAuthenticatedUserWithInvalidId()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-id"),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "testuser")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private string HashPassword(string password)
    {
        var salt = _configuration["Auth:Salt"] ?? "DefaultSalt123!";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(hashedBytes);
    }
}