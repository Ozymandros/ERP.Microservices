using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Auth.Infrastructure.Services;
using System.Security.Claims;

namespace MyApp.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenProvider jwtTokenProvider,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenProvider = jwtTokenProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || user.IsExternalLogin)
        {
            _logger.LogWarning("Login attempt failed for email: {Email}", loginDto.Email);
            return null;
        }

        var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!result)
        {
            _logger.LogWarning("Invalid password for user: {Email}", loginDto.Email);
            return null;
        }

        return await GenerateTokenResponseAsync(user);
    }

    public async Task<TokenResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", registerDto.Email);
            return null;
        }

        var user = new ApplicationUser
        {
            Email = registerDto.Email,
            UserName = registerDto.Username,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for user: {Email}", registerDto.Email);
            return null;
        }

        // Assign default "User" role if it exists
        await _userManager.AddToRoleAsync(user, "User");

        return await GenerateTokenResponseAsync(user);
    }

    public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        var principal = _jwtTokenProvider.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
        if (principal == null)
        {
            _logger.LogWarning("Invalid expired token provided");
            return null;
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Cannot extract user ID from token");
            return null;
        }

        var refreshToken = await _refreshTokenRepository.GetValidRefreshTokenAsync(userId, refreshTokenDto.RefreshToken);
        if (refreshToken == null)
        {
            _logger.LogWarning("Invalid or expired refresh token for user: {UserId}", userId);
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return null;
        }

        return await GenerateTokenResponseAsync(user);
    }

    public async Task<TokenResponseDto?> ExternalLoginAsync(ExternalLoginDto externalLoginDto)
    {
        // Try to find existing user with external provider
        var user = await _userRepository.GetByExternalIdAsync(externalLoginDto.Provider, externalLoginDto.ExternalId);

        if (user == null)
        {
            // Create new user for external login
            user = new ApplicationUser
            {
                Email = externalLoginDto.Email,
                UserName = externalLoginDto.Email.Split('@')[0] + "_" + externalLoginDto.Provider,
                FirstName = externalLoginDto.FirstName,
                LastName = externalLoginDto.LastName,
                IsExternalLogin = true,
                ExternalProvider = externalLoginDto.Provider,
                ExternalId = externalLoginDto.ExternalId,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to create user for external provider: {Provider}", externalLoginDto.Provider);
                return null;
            }

            // Assign default "User" role
            await _userManager.AddToRoleAsync(user, "User");
        }

        return await GenerateTokenResponseAsync(user);
    }

    public async Task LogoutAsync(Guid userId)
    {
        await _refreshTokenRepository.RevokeUserTokensAsync(userId);
        _logger.LogInformation("User logged out: {UserId}", userId);
    }

    private async Task<TokenResponseDto> GenerateTokenResponseAsync(ApplicationUser user)
    {
        // ⭐️ Step 1: Retrieve user Roles and Claims
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user); // Call to get the claims

        // ⭐️ Step 2: Generate Access Token
        // Assume that your provider accepts the list of roles and claims to include them in the JWT.
        var accessToken = await _jwtTokenProvider.GenerateAccessTokenAsync(user, roles, claims);

        // Generar Refresh Token
        var refreshToken = _jwtTokenProvider.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        // ⭐️ Step 3: Return DTO with roles included
        var userDto = new UserDto(user.Id)
        {
            CreatedAt = user.CreatedAt,
            CreatedBy = "",
            UpdatedAt = user.UpdatedAt,
            UpdatedBy = null,
            Email = user.Email,
            Username = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsExternalLogin = user.IsExternalLogin,
            ExternalProvider = user.ExternalProvider,
            Roles = new List<RoleDto?>(),
            Permissions = new List<PermissionDto?>(),
            IsAdmin = false,
            IsActive = user.IsActive
        };

        return new TokenResponseDto(accessToken, refreshToken, 15 * 60, "Bearer", userDto);
    }
}
