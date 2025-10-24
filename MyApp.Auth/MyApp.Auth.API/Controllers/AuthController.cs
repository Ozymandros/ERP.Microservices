using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;

namespace MyApp.Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
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
                _logger.LogWarning("Login failed for email: {Email}", loginDto.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            _logger.LogInformation("User logged in: {Email}", loginDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for email: {Email}", loginDto.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
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
                _logger.LogWarning("Registration failed for email: {Email}", registerDto.Email);
                return Conflict(new { message = "Email already exists" });
            }

            _logger.LogInformation("User registered: {Email}", registerDto.Email);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for email: {Email}", registerDto.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpOptions("refresh")]
    [AllowAnonymous] 
    public IActionResult HandleRefreshOptions()
    {
        // Retornem un 204 No Content. 
        // El middleware CORS ara tindrà l'oportunitat d'afegir les capçaleres abans de finalitzar la resposta.
        return NoContent();
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if(refreshTokenDto is null || string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken) || string.IsNullOrWhiteSpace(refreshTokenDto.AccessToken))
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
    /// Initiate external login (Google, Microsoft, Apple, GitHub)
    /// </summary>
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
    /// Handle external login callback
    /// </summary>
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

            var externalLoginDto = new ExternalLoginDto
            {
                Provider = authProvider,
                ExternalId = externalId,
                Email = email,
                FirstName = givenName,
                LastName = surname
            };

            var tokenResponse = await _authService.ExternalLoginAsync(externalLoginDto);
            if (tokenResponse == null)
            {
                _logger.LogWarning("External login failed for provider: {Provider}", authProvider);
                return Unauthorized(new { message = "External login failed" });
            }

            _logger.LogInformation("User logged in via external provider: {Provider}", authProvider);
            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login callback error");
            return StatusCode(500, new { message = "An error occurred during external login" });
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
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
            _logger.LogInformation("User logged out: {UserId}", userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }
}
