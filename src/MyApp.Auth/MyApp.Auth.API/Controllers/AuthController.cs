using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.API.Authorization;
using Microsoft.AspNetCore.Authentication;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Domain.Security;

using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Auth.API.Controllers;

/// <summary>
/// Handles authentication operations including login, registration, token refresh, external login, and logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogSanitizer _logSanitizer;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// </summary>
    /// <param name="authService">The auth Service.</param>
    /// <param name="logSanitizer">The log Sanitizer.</param>
    /// <param name="logger">The logger.</param>
    public AuthController(
        IAuthService authService,
        ILogSanitizer logSanitizer,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logSanitizer = logSanitizer;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password and returns JWT tokens.
    /// </summary>
    /// <param name="loginDto">The login Dto.</param>
    /// <returns>A <see cref="TokenResponseDto"/> on success, or an error response.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                _logger.LogWarning(
                    "Login failed for user: {@User}",
                    new { Email = _logSanitizer.Sanitize(loginDto.Email) });
                return Unauthorized(new { message = "Invalid email or password" });
            }

            _logger.LogInformation(
                "User logged in: {@User}",
                new { Email = _logSanitizer.Sanitize(loginDto.Email) });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Login error for user: {@User}",
                new { Email = _logSanitizer.Sanitize(loginDto.Email) });
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Registers a new user account and returns JWT tokens.
    /// </summary>
    /// <param name="registerDto">The register Dto.</param>
    /// <returns>A <see cref="TokenResponseDto"/> on success, or an error response.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            if (result == null)
            {
                _logger.LogWarning(
                    "Registration failed for user: {@User}",
                    new { Email = _logSanitizer.Sanitize(registerDto.Email) });
                return Conflict(new { message = "Email already exists" });
            }

            _logger.LogInformation(
                "User registered: {@User}",
                new { Email = _logSanitizer.Sanitize(registerDto.Email) });
            return CreatedAtAction(nameof(Register), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Registration error for user: {@User}",
                new { Email = _logSanitizer.Sanitize(registerDto.Email) });
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Handles OPTIONS pre-flight requests for the refresh endpoint to support CORS.
    /// </summary>
    /// <returns>A 204 No Content response.</returns>
    [HttpOptions("refresh")]
    [AllowAnonymous]
    public IActionResult HandleRefreshOptions()
    {
        // Return a 204 No Content.
        // The CORS middleware now has a chance to add headers before the response is finalized.
        return NoContent();
    }

    /// <summary>
    /// Issues new JWT tokens using a valid refresh token.
    /// </summary>
    /// <param name="refreshTokenDto">The refresh Token Dto.</param>
    /// <returns>A new <see cref="TokenResponseDto"/> on success, or an error response.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (refreshTokenDto is null || string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken) || string.IsNullOrWhiteSpace(refreshTokenDto.AccessToken))
            return BadRequest(new { message = "Invalid token data" });

        try
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            if (result == null)
            {
                _logger.LogWarning("Token refresh failed");
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Initiates an external OAuth login by redirecting to the specified provider.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>A redirect challenge to the provider's login page, or a 400 response for invalid providers.</returns>
    [HttpGet("external-login/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ExternalLogin(string provider)
    {
        var allowedProviders = new[] { "google", "microsoft", "apple", "github" };
        if (!allowedProviders.Contains(provider.ToLower()))
            return BadRequest(new { message = "Invalid provider" });

        // Redirect to external provider's login page
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl = "/" });
        return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl
        }, provider);
    }

    /// <summary>
    /// Handles the OAuth callback from an external provider, authenticating or creating the user, and returning JWT tokens.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <param name="returnUrl">The return Url.</param>
    /// <returns>A <see cref="TokenResponseDto"/> on success, or an error response.</returns>
    [HttpGet("external-callback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExternalLoginCallback(string? provider = null, string? returnUrl = null)
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync();
            if (result?.Principal == null)
                return BadRequest(new { message = "External authentication failed" });

            var externalId = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
            var givenName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var surname = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;
            var authProvider = provider ?? "unknown";

            if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Missing required external login information" });

            var externalLoginDto = new ExternalLoginDto(authProvider, externalId, email, givenName, surname);

            var tokenResponse = await _authService.ExternalLoginAsync(externalLoginDto);
            if (tokenResponse == null)
            {
                _logger.LogWarning("External login failed for provider: {@Provider}", new { Provider = authProvider });
                return Unauthorized(new { message = "External login failed" });
            }

            _logger.LogInformation("User logged in via external provider: {@Provider}", new { Provider = authProvider });
            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login callback error");
            return StatusCode(500, new { message = "An error occurred during external login" });
        }
    }

    /// <summary>
    /// Logs out the currently authenticated user by revoking all their refresh tokens.
    /// </summary>
    /// <returns>204 No Content on success, or 401 if the user is not authenticated.</returns>
    [HttpPost("logout")]
    [AuthorizeJwt]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            await _authService.LogoutAsync(userId);
            _logger.LogInformation("User logged out: {@User}", new { UserId = userId });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }
}
