using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Contracts.Services;

public interface IAuthService
{
    Task<TokenResponseDto?> LoginAsync(LoginDto loginDto);
    Task<TokenResponseDto?> RegisterAsync(RegisterDto registerDto);
    Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<TokenResponseDto?> ExternalLoginAsync(ExternalLoginDto externalLoginDto);
    Task LogoutAsync(Guid userId);
}
