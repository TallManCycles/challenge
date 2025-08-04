using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Models;
using backend.Services;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT authentication
public class GarminOAuthController : ControllerBase
{
    private readonly IGarminOAuthService _garminOAuthService;
    private readonly ILogger<GarminOAuthController> _logger;
    private readonly GarminOAuthConfig _config;

    public GarminOAuthController(IGarminOAuthService garminOAuthService, ILogger<GarminOAuthController> logger, IOptions<GarminOAuthConfig> config)
    {
        _garminOAuthService = garminOAuthService;
        _logger = logger;
        _config = config.Value;
    }

    [HttpGet("initiate")]
    public async Task<IActionResult> InitiateOAuth()
    {
        try
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
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
        [FromQuery] string? state = null)
    {
        try
        {
            if (string.IsNullOrEmpty(oauth_token) || 
                string.IsNullOrEmpty(oauth_verifier))
            {
                return BadRequest("Missing required OAuth parameters");
            }

            bool success = await _garminOAuthService.HandleCallbackAsync(state, oauth_token, oauth_verifier);
            
            if (success)
            {
                // Redirect to frontend success page
                // pull the frontend url from the appsettings.json file.
                return Redirect($"{_config.RedirectUrl}/settings?garmin=success");
            }
            else
            {
                // Redirect to frontend error page
                return Redirect($"{_config.RedirectUrl}/settings?garmin=error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Garmin OAuth callback");
            return Redirect($"{_config.RedirectUrl}/settings?garmin=error");
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetOAuthStatus()
    {
        try
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
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

    [HttpPost("disconnect")]
    public async Task<IActionResult> DisconnectGarmin()
    {
        try
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("User ID not found in token");
            }

            bool success = await _garminOAuthService.DisconnectGarminAsync(userId);
            
            if (success)
            {
                return Ok(new { message = "Garmin disconnected successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to disconnect Garmin" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Garmin");
            return StatusCode(500, new { error = "Failed to disconnect Garmin" });
        }
    }
}