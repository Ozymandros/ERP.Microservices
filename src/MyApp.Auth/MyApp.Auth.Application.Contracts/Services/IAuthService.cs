using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Contracts.Services;

/// <summary>
/// Defines operations for user authentication and token management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// </summary>
    Task<TokenResponseDto?> LoginAsync(LoginDto loginDto);

    /// <summary>
    /// Registers a new user account and returns authentication tokens.
    /// </summary>
    Task<TokenResponseDto?> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Generates new authentication tokens using a valid refresh token.
    /// </summary>
    Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

    /// <summary>
    /// Authenticates a user through an external authentication provider.
    /// </summary>
    Task<TokenResponseDto?> ExternalLoginAsync(ExternalLoginDto externalLoginDto);

    /// <summary>
    /// Logs out a user by revoking all of their refresh tokens.
    /// </summary>
    Task LogoutAsync(Guid userId);
}
