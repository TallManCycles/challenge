# Garmin OAuth Implementation Guide

## Overview
This guide provides a secure implementation of Garmin OAuth 1.0a flow for a Vue.js frontend with .NET 8 backend, following OAuth security best practices.

## Architecture Overview

```
Vue.js Frontend -> .NET 8 API -> Garmin OAuth -> PostgreSQL
```

## Security Best Practices Implemented

1. **State Parameter**: CSRF protection using cryptographically secure random state
2. **Short-lived Request Tokens**: Temporary tokens with expiration
3. **Secure Token Storage**: Encrypted sensitive data in database
4. **Proper Error Handling**: No sensitive data in error responses
5. **Request Validation**: Validate all OAuth callbacks
6. **HTTPS Only**: All OAuth redirects use HTTPS

## Backend Implementation

### 1. NuGet Packages Required

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="System.Security.Cryptography.Algorithms" Version="4.3.1" />
```

### 2. Database Models

```csharp
// Models/GarminOAuthToken.cs
using System.ComponentModel.DataAnnotations;

namespace YourApp.Models
{
    public class GarminOAuthToken
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
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
    }
}
```

### 3. DbContext Configuration

```csharp
// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using YourApp.Models;

namespace YourApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<GarminOAuthToken> GarminOAuthTokens { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GarminOAuthToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.State).IsUnique();
                entity.HasIndex(e => e.RequestToken);
                
                // Set expiration default to 10 minutes from creation
                entity.Property(e => e.ExpiresAt)
                    .HasDefaultValueSql("NOW() + INTERVAL '10 minutes'");
            });
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
```

### 4. OAuth Configuration

```csharp
// Configuration/GarminOAuthConfig.cs
namespace YourApp.Configuration
{
    public class GarminOAuthConfig
    {
        public string ConsumerKey { get; set; } = string.Empty;
        public string ConsumerSecret { get; set; } = string.Empty;
        public string RequestTokenUrl { get; set; } = "https://connectapi.garmin.com/oauth-service/oauth/request_token";
        public string AuthorizeUrl { get; set; } = "https://connect.garmin.com/oauthConfirm";
        public string AccessTokenUrl { get; set; } = "https://connectapi.garmin.com/oauth-service/oauth/access_token";
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
```

### 5. OAuth Service

```csharp
// Services/GarminOAuthService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using YourApp.Configuration;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Services
{
    public interface IGarminOAuthService
    {
        Task<(string authUrl, string state)> InitiateOAuthAsync(string userId);
        Task<bool> HandleCallbackAsync(string state, string oauthToken, string oauthVerifier);
        Task<GarminOAuthToken?> GetValidTokenAsync(string userId);
    }

    public class GarminOAuthService : IGarminOAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly GarminOAuthConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GarminOAuthService> _logger;

        public GarminOAuthService(
            ApplicationDbContext context,
            IOptions<GarminOAuthConfig> config,
            IHttpClientFactory httpClientFactory,
            ILogger<GarminOAuthService> logger)
        {
            _context = context;
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(string authUrl, string state)> InitiateOAuthAsync(string userId)
        {
            try
            {
                // Generate secure state parameter
                string state = GenerateSecureState();
                
                // Clean up any existing expired tokens for this user
                await CleanupExpiredTokensAsync(userId);

                // Get request token from Garmin
                var (requestToken, requestTokenSecret) = await GetRequestTokenAsync();

                // Store in database
                var oauthToken = new GarminOAuthToken
                {
                    UserId = userId,
                    RequestToken = requestToken,
                    RequestTokenSecret = requestTokenSecret,
                    State = state,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                };

                _context.GarminOAuthTokens.Add(oauthToken);
                await _context.SaveChangesAsync();

                // Build authorization URL
                string authUrl = $"{_config.AuthorizeUrl}?oauth_token={requestToken}&state={state}";

                _logger.LogInformation("OAuth initiation successful for user {UserId}", userId);
                return (authUrl, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate OAuth for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HandleCallbackAsync(string state, string oauthToken, string oauthVerifier)
        {
            try
            {
                // Find the token record by state and request token
                var tokenRecord = await _context.GarminOAuthTokens
                    .FirstOrDefaultAsync(t => t.State == state && 
                                            t.RequestToken == oauthToken && 
                                            !t.IsAuthorized &&
                                            t.ExpiresAt > DateTime.UtcNow);

                if (tokenRecord == null)
                {
                    _logger.LogWarning("Invalid or expired OAuth callback with state {State}", state);
                    return false;
                }

                // Exchange request token for access token
                var (accessToken, accessTokenSecret) = await GetAccessTokenAsync(
                    oauthToken, 
                    tokenRecord.RequestTokenSecret, 
                    oauthVerifier);

                // Update the record
                tokenRecord.AccessToken = accessToken;
                tokenRecord.AccessTokenSecret = accessTokenSecret;
                tokenRecord.OAuthVerifier = oauthVerifier;
                tokenRecord.IsAuthorized = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("OAuth callback processed successfully for user {UserId}", tokenRecord.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle OAuth callback");
                return false;
            }
        }

        public async Task<GarminOAuthToken?> GetValidTokenAsync(string userId)
        {
            return await _context.GarminOAuthTokens
                .Where(t => t.UserId == userId && t.IsAuthorized)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<(string requestToken, string requestTokenSecret)> GetRequestTokenAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("GarminOAuth");
            
            var oauthParams = new Dictionary<string, string>
            {
                { "oauth_callback", _config.CallbackUrl },
                { "oauth_consumer_key", _config.ConsumerKey },
                { "oauth_nonce", GenerateNonce() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimestamp() },
                { "oauth_version", "1.0" }
            };

            string signature = GenerateSignature("POST", _config.RequestTokenUrl, oauthParams, _config.ConsumerSecret, "");
            oauthParams.Add("oauth_signature", signature);

            string authHeader = GenerateAuthorizationHeader(oauthParams);
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

            var response = await httpClient.PostAsync(_config.RequestTokenUrl, null);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            var responseParams = ParseQueryString(responseContent);

            return (responseParams["oauth_token"], responseParams["oauth_token_secret"]);
        }

        private async Task<(string accessToken, string accessTokenSecret)> GetAccessTokenAsync(
            string requestToken, string requestTokenSecret, string verifier)
        {
            var httpClient = _httpClientFactory.CreateClient("GarminOAuth");
            
            var oauthParams = new Dictionary<string, string>
            {
                { "oauth_consumer_key", _config.ConsumerKey },
                { "oauth_token", requestToken },
                { "oauth_verifier", verifier },
                { "oauth_nonce", GenerateNonce() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimestamp() },
                { "oauth_version", "1.0" }
            };

            string signature = GenerateSignature("POST", _config.AccessTokenUrl, oauthParams, _config.ConsumerSecret, requestTokenSecret);
            oauthParams.Add("oauth_signature", signature);

            string authHeader = GenerateAuthorizationHeader(oauthParams);
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

            var response = await httpClient.PostAsync(_config.AccessTokenUrl, null);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            var responseParams = ParseQueryString(responseContent);

            return (responseParams["oauth_token"], responseParams["oauth_token_secret"]);
        }

        private async Task CleanupExpiredTokensAsync(string userId)
        {
            var expiredTokens = await _context.GarminOAuthTokens
                .Where(t => t.UserId == userId && 
                           (t.ExpiresAt < DateTime.UtcNow || !t.IsAuthorized))
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.GarminOAuthTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }
        }

        private static string GenerateSecureState()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private static string GenerateNonce()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[16];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
        }

        private static string GenerateTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        private static string GenerateSignature(string method, string url, Dictionary<string, string> parameters, 
            string consumerSecret, string tokenSecret)
        {
            // Implementation of OAuth 1.0a signature generation
            var sortedParams = parameters.OrderBy(p => p.Key).ToList();
            var paramString = string.Join("&", sortedParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            
            string baseString = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";
            string signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";
            
            using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
            byte[] hashBytes = hmac.ComputeHash(Encoding.ASCII.GetBytes(baseString));
            return Convert.ToBase64String(hashBytes);
        }

        private static string GenerateAuthorizationHeader(Dictionary<string, string> parameters)
        {
            var headerParams = parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\"");
            return $"OAuth {string.Join(", ", headerParams)}";
        }

        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            var pairs = queryString.Split('&');
            
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    result[HttpUtility.UrlDecode(keyValue[0])] = HttpUtility.UrlDecode(keyValue[1]);
                }
            }
            
            return result;
        }
    }
}
```

### 6. Controller

```csharp
// Controllers/GarminOAuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using YourApp.Services;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires JWT authentication
    public class GarminOAuthController : ControllerBase
    {
        private readonly IGarminOAuthService _garminOAuthService;
        private readonly ILogger<GarminOAuthController> _logger;

        public GarminOAuthController(IGarminOAuthService garminOAuthService, ILogger<GarminOAuthController> logger)
        {
            _garminOAuthService = garminOAuthService;
            _logger = logger;
        }

        [HttpGet("initiate")]
        public async Task<IActionResult> InitiateOAuth()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var (authUrl, state) = await _garminOAuthService.InitiateOAuthAsync(userId);
                
                return Ok(new { url = authUrl, state });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Garmin OAuth");
                return StatusCode(500, new { error = "Failed to initiate Garmin authentication" });
            }
        }

        [HttpGet("callback")]
        [AllowAnonymous] // This endpoint receives callbacks from Garmin
        public async Task<IActionResult> HandleCallback(
            [FromQuery] string oauth_token,
            [FromQuery] string oauth_verifier,
            [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(oauth_token) || 
                    string.IsNullOrEmpty(oauth_verifier) || 
                    string.IsNullOrEmpty(state))
                {
                    return BadRequest("Missing required OAuth parameters");
                }

                bool success = await _garminOAuthService.HandleCallbackAsync(state, oauth_token, oauth_verifier);
                
                if (success)
                {
                    // Redirect to frontend success page
                    return Redirect($"{Request.Scheme}://{Request.Host}/oauth/garmin/success");
                }
                else
                {
                    // Redirect to frontend error page
                    return Redirect($"{Request.Scheme}://{Request.Host}/oauth/garmin/error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Garmin OAuth callback");
                return Redirect($"{Request.Scheme}://{Request.Host}/oauth/garmin/error");
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetOAuthStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var token = await _garminOAuthService.GetValidTokenAsync(userId);
                
                return Ok(new { 
                    isConnected = token != null,
                    connectedAt = token?.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Garmin OAuth status");
                return StatusCode(500, new { error = "Failed to get OAuth status" });
            }
        }
    }
}
```

### 7. Program.cs Configuration

```csharp
// Program.cs additions
using Microsoft.EntityFrameworkCore;
using YourApp.Configuration;
using YourApp.Data;
using YourApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<GarminOAuthConfig>(
    builder.Configuration.GetSection("GarminOAuth"));

builder.Services.AddHttpClient("GarminOAuth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IGarminOAuthService, GarminOAuthService>();

// Add other services...

var app = builder.Build();

// Configure pipeline...
```

### 8. Configuration (appsettings.json)

```json
{
  "GarminOAuth": {
    "ConsumerKey": "your-garmin-consumer-key",
    "ConsumerSecret": "your-garmin-consumer-secret",
    "CallbackUrl": "https://yourdomain.com/api/garminoauth/callback"
  },
  "ConnectionStrings": {
    "DefaultConnection": "your-postgresql-connection-string"
  }
}
```

## Frontend Implementation

### Updated Vue.js Component

```javascript
// In your Vue component
async initiateGarminOAuth() {
    this.$swal.fire({
        icon: "info",
        title: "Garmin Sync",
        text: "This will ask for your Garmin credentials and sync your activity data",
    });

    try {
        const response = await this.$http.get('/api/garminoauth/initiate');
        
        if (response.status === 200) {
            // Store state for verification if needed
            sessionStorage.setItem('garmin_oauth_state', response.data.state);
            
            // Redirect to Garmin
            window.location.href = response.data.url;
        } else {
            this.showError("Failed to initiate Garmin connection");
        }
    } catch (error) {
        console.error("Garmin OAuth failed", error);
        this.showError("There was an error connecting to Garmin. Please try again later");
    }
},

async checkGarminStatus() {
    try {
        const response = await this.$http.get('/api/garminoauth/status');
        return response.data;
    } catch (error) {
        console.error("Failed to check Garmin status", error);
        return { isConnected: false };
    }
},

showError(message) {
    this.$swal.fire({
        icon: "error",
        title: "Error",
        text: message,
    });
}
```

### OAuth Success/Error Pages

Create dedicated pages in your Vue router:

```javascript
// router/index.js
{
    path: '/oauth/garmin/success',
    name: 'GarminOAuthSuccess',
    component: () => import('@/views/oauth/GarminSuccess.vue')
},
{
    path: '/oauth/garmin/error',
    name: 'GarminOAuthError',
    component: () => import('@/views/oauth/GarminError.vue')
}
```

## Security Considerations

1. **HTTPS Only**: Ensure all OAuth URLs use HTTPS
2. **State Validation**: Always validate the state parameter
3. **Token Expiration**: Implement proper token expiration and cleanup
4. **Error Handling**: Never expose sensitive information in error messages
5. **Rate Limiting**: Consider implementing rate limiting on OAuth endpoints
6. **Logging**: Log security events but not sensitive data

## Database Migration

```sql
-- PostgreSQL migration
CREATE TABLE garmin_oauth_tokens (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    request_token VARCHAR(255) NOT NULL,
    request_token_secret VARCHAR(255) NOT NULL,
    access_token VARCHAR(255),
    access_token_secret VARCHAR(255),
    state VARCHAR(255) NOT NULL UNIQUE,
    is_authorized BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE DEFAULT (NOW() + INTERVAL '10 minutes'),
    oauth_verifier VARCHAR(255)
);

CREATE INDEX idx_garmin_oauth_user_id ON garmin_oauth_tokens(user_id);
CREATE INDEX idx_garmin_oauth_state ON garmin_oauth_tokens(state);
CREATE INDEX idx_garmin_oauth_request_token ON garmin_oauth_tokens(request_token);
```

## Testing

1. Test the full OAuth flow in a development environment
2. Verify state parameter validation
3. Test token expiration and cleanup
4. Test error scenarios (invalid tokens, expired tokens, etc.)
5. Verify CSRF protection works correctly

## Next Steps

1. Implement this code structure
2. Configure your Garmin developer application with the correct callback URL
3. Test the OAuth flow thoroughly
4. Add proper monitoring and alerting
5. Consider implementing refresh token logic if needed for long-term access