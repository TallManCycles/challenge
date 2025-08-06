using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using backend.Data;
using backend.Models;

namespace backend.Services;

public interface IGarminOAuthService
{
    Task<(string authUrl, string state)> InitiateOAuthAsync(int userId);
    Task<bool> HandleCallbackAsync(string? state, string oauthToken, string oauthVerifier);
    Task<GarminOAuthToken?> GetValidTokenAsync(int userId);
    Task<bool> DisconnectGarminAsync(int userId);
}

public class GarminOAuthService : IGarminOAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly GarminOAuthConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileLoggingService _logger;

    public GarminOAuthService(
        ApplicationDbContext context,
        IOptions<GarminOAuthConfig> config,
        IHttpClientFactory httpClientFactory,
        IFileLoggingService logger)
    {
        _context = context;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(string authUrl, string state)> InitiateOAuthAsync(int userId)
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

            await _logger.LogInfoAsync($"OAuth initiation successful for user {userId}");
            return (authUrl, state);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Failed to initiate OAuth for user {UserId}", ex, "GarminOAuthService");
            throw;
        }
    }

    public async Task<bool> HandleCallbackAsync(string? state, string oauthToken, string oauthVerifier)
    {
        try
        {
            // Find the token record by request token (and state if provided)
            var tokenRecord = await _context.GarminOAuthTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.RequestToken == oauthToken && 
                                        !t.IsAuthorized &&
                                        t.ExpiresAt > DateTime.UtcNow &&
                                        (string.IsNullOrEmpty(state) || t.State == state));

            if (tokenRecord == null)
            {
                await _logger.LogWarningAsync($"Invalid or expired OAuth callback with state {state}");
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

            // Update User model with Garmin connection info
            tokenRecord.User.GarminAccessToken = accessToken;
            tokenRecord.User.GarminRefreshToken = accessTokenSecret; // OAuth 1.0a uses token secret
            tokenRecord.User.GarminConnectedAt = DateTime.UtcNow;
            tokenRecord.User.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _logger.LogInfoAsync($"OAuth callback processed successfully for user {tokenRecord.UserId}");
            return true;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Failed to handle OAuth callback", ex, "GarminOAuthService");
            return false;
        }
    }

    public async Task<GarminOAuthToken?> GetValidTokenAsync(int userId)
    {
        return await _context.GarminOAuthTokens
            .Where(t => t.UserId == userId && t.IsAuthorized)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DisconnectGarminAsync(int userId)
    {
        try
        {
            // Remove all OAuth tokens for this user
            var tokens = await _context.GarminOAuthTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (tokens.Any())
            {
                _context.GarminOAuthTokens.RemoveRange(tokens);
            }

            // Clear Garmin data from User model
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.GarminUserId = null;
                user.GarminAccessToken = null;
                user.GarminRefreshToken = null;
                user.GarminConnectedAt = null;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            await _logger.LogInfoAsync($"Garmin disconnected successfully for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Failed to disconnect Garmin for user {UserId}", ex, "GarminOAuthService");
            return false;
        }
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

    private async Task CleanupExpiredTokensAsync(int userId)
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