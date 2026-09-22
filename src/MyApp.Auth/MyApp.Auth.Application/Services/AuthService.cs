using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Auth.Infrastructure.Services;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Security;
using System.Security.Claims;

namespace MyApp.Auth.Application.Services;

/// <summary>
/// Provides authentication and authorization services including login, registration, token refresh, and logout operations.
/// </summary>
public class AuthService : AppServiceBase, IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogSanitizer _logSanitizer;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthService class.
    /// </summary>
    /// <param name="userManager">The user Manager.</param>
    /// <param name="jwtTokenProvider">The jwt Token Provider.</param>
    /// <param name="refreshTokenRepository">The refresh Token Repository.</param>
    /// <param name="userRepository">The user Repository.</param>
    /// <param name="roleRepository">The role Repository.</param>
    /// <param name="permissionRepository">The permission Repository.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logSanitizer">The log Sanitizer.</param>
    /// <param name="logger">The logger.</param>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenProvider jwtTokenProvider,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogSanitizer logSanitizer,
        ILogger<AuthService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Auth)
    {
        _userManager = userManager;
        _jwtTokenProvider = jwtTokenProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _logSanitizer = logSanitizer;
        _logger = logger;
    }

    /// <summary>
    /// Login asynchronously.
    /// </summary>
    /// <param name="loginDto">The login Dto.</param>
    /// <returns>A <see cref="TokenResponseDto"/> with tokens on success, or <c>null</c> if authentication fails.</returns>
    public async Task<TokenResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || user.IsExternalLogin)
        {
            _logger.LogWarning(
                "Login attempt failed for email: {@Login}",
                new { Email = _logSanitizer.Sanitize(loginDto.Email) });
            return null;
        }

        var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!result)
        {
            _logger.LogWarning(
                "Invalid password for user: {@Login}",
                new { Email = _logSanitizer.Sanitize(loginDto.Email) });
            return null;
        }

        return await GenerateTokenResponseAsync(user);
    }

    /// <summary>
    /// Register asynchronously.
    /// </summary>
    /// <param name="registerDto">The register Dto.</param>
    /// <returns>A <see cref="TokenResponseDto"/> with tokens on success, or <c>null</c> if registration fails.</returns>
    public async Task<TokenResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning(
                "Registration attempt with existing email: {@Registration}",
                new { Email = _logSanitizer.Sanitize(registerDto.Email) });
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
            _logger.LogWarning(
                "Registration failed for user: {@Registration}",
                new { Email = _logSanitizer.Sanitize(registerDto.Email) });
            return null;
        }

        // Assign default "User" role if it exists
        await _userManager.AddToRoleAsync(user, "User");

        return await GenerateTokenResponseAsync(user);
    }

    /// <summary>
    /// Refresh token asynchronously.
    /// </summary>
    /// <param name="refreshTokenDto">The refresh Token Dto.</param>
    /// <returns>A new <see cref="TokenResponseDto"/> with fresh tokens, or <c>null</c> if the refresh token is invalid or expired.</returns>
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

    /// <summary>
    /// External login asynchronously.
    /// </summary>
    /// <param name="externalLoginDto">The external Login Dto.</param>
    /// <returns>A <see cref="TokenResponseDto"/> with tokens on success, or <c>null</c> if user creation fails.</returns>
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
                _logger.LogWarning(
                    "Failed to create user for external provider: {@ExternalLogin}",
                    new { Provider = _logSanitizer.Sanitize(externalLoginDto.Provider) });
                return null;
            }

            // Assign default "User" role
            await _userManager.AddToRoleAsync(user, "User");
        }

        return await GenerateTokenResponseAsync(user);
    }

    /// <summary>
    /// Logout asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    public async Task LogoutAsync(Guid userId)
    {
        await _refreshTokenRepository.RevokeUserTokensAsync(userId);
        await SaveChangesAsync();
        _logger.LogInformation("User logged out: {UserId}", userId);
    }

    /// <summary>
    /// Generates an access token and refresh token for the specified user, including roles and permissions claims.
    /// </summary>
    /// <param name="user">The user for whom to generate the token response.</param>
    /// <returns>A <see cref="TokenResponseDto"/> containing access token, refresh token, and user information.</returns>
    private async Task<TokenResponseDto> GenerateTokenResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var identityClaims = await _userManager.GetClaimsAsync(user);

        var userRoles = await _roleRepository.GetRolesByUserIdAsync(user.Id);
        var roleNames = userRoles.Select(r => r.Name).ToList();
        bool isAdmin = userRoles.Any(r => r.Name != null && r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        _logger.LogInformation("User {UserId} has roles: {Roles}, IsAdmin: {IsAdmin}",
            user.Id, string.Join(", ", roleNames), isAdmin);

        List<Permission> permissions;
        if (isAdmin)
        {
            var allPermissions = await _permissionRepository.GetAllAsync();
            permissions = allPermissions.ToList();
            _logger.LogInformation("Admin user {UserId} - returning all {Count} permissions",
                user.Id, permissions.Count);
        }
        else
        {
            permissions = new List<Permission>();
            foreach (var role in userRoles)
            {
                var rolePermissions = await _roleRepository.GetPermissionsForRoleAsync(role.Id);
                permissions.AddRange(rolePermissions);
                _logger.LogInformation("Role {RoleName} ({RoleId}) has {Count} permissions",
                    _logSanitizer.Sanitize(role.Name), role.Id, rolePermissions.Count());
            }

            var allUserPermissions = await _permissionRepository.GetAllPermissionsByUserId(user.Id);
            var rolePermissionIds = permissions.Select(p => p.Id).ToHashSet();
            var directUserPermissions = allUserPermissions.Where(p => !rolePermissionIds.Contains(p.Id));
            permissions.AddRange(directUserPermissions);

            var distinctPermissions = permissions.DistinctBy(p => p.Id).ToList();
            _logger.LogInformation("User {UserId} - {DirectCount} direct permissions, {RoleCount} role permissions, {TotalCount} total distinct permissions",
                user.Id, directUserPermissions.Count(),
                permissions.Count - directUserPermissions.Count(),
                distinctPermissions.Count);

            permissions = distinctPermissions;
        }

        var permissionClaims = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p.Module) && !string.IsNullOrWhiteSpace(p.Action))
            .Select(p => new Claim(HasPermissionAttribute.PermissionClaimType, $"{p.Module}:{p.Action}"))
            .ToList();

        var tokenClaims = identityClaims.Concat(permissionClaims).ToList();
        var accessToken = await _jwtTokenProvider.GenerateAccessTokenAsync(user, roles, tokenClaims);

        var refreshToken = _jwtTokenProvider.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);
        await SaveChangesAsync();

        var roleDtos = userRoles.Select(r => new RoleDto(r.Id)
        {
            Name = r.Name,
            Description = r.Description
        }).Cast<RoleDto?>().ToList();

        var permissionDtos = permissions
            .DistinctBy(p => p.Id)
            .Select(p => new PermissionDto(p.Id)
            {
                Module = p.Module,
                Action = p.Action,
                Description = p.Description
            })
            .Cast<PermissionDto?>().ToList();

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
            Roles = roleDtos!,
            Permissions = permissionDtos!,
            IsAdmin = isAdmin,
            IsActive = user.IsActive
        };

        return new TokenResponseDto(accessToken, refreshToken, 15 * 60, "Bearer", userDto);
    }
}
