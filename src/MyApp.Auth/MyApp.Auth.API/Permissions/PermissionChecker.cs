using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Auth.API.Permissions;

/// <summary>
/// Provides Permission Checker functionality. Checks if a user has permission to perform an action based on their roles and permissions.
/// </summary>
public class PermissionChecker : IPermissionChecker
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the PermissionChecker class.
    /// </summary>
    /// <param name="userManager">The user Manager.</param>
    /// <param name="permissionRepository">The permission Repository.</param>
    /// <param name="httpContextAccessor">The http Context Accessor.</param>
    public PermissionChecker(
        UserManager<ApplicationUser> userManager,
        IPermissionRepository permissionRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _permissionRepository = permissionRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Determines whether permission asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="module">The module.</param>
    /// <param name="action">The action.</param>
    /// <returns><c>true</c> if the user has the permission; otherwise, <c>false</c>.</returns>
    public async Task<bool> HasPermissionAsync(Guid userId, string module, string action)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || string.IsNullOrWhiteSpace(user.UserName))
        {
            return false;
        }

        var userPermissions = await _permissionRepository.GetByUserName(user.UserName, module, action);
        if (userPermissions.Any())
            return true;

        var roles = await _userManager.GetRolesAsync(user);

        foreach (var roleName in roles)
        {
            var rolePermissions = await _permissionRepository.GetByRoleName(roleName, module, action);
            if (rolePermissions.Any())
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether permission asynchronously.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="action">The action.</param>
    /// <returns><c>true</c> if the current user has the permission; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="module"/> or <paramref name="action"/> is null or empty.</exception>
    public async Task<bool> HasPermissionAsync(string module, string action)
    {
        if (string.IsNullOrEmpty(module))
            throw new ArgumentException($"'{nameof(module)}' cannot be null or empty.", nameof(module));
        if (string.IsNullOrEmpty(action))
            throw new ArgumentException($"'{nameof(action)}' cannot be null or empty.", nameof(action));

        // Get current user from HttpContext
        if (_httpContextAccessor.HttpContext?.User?.Identity?.Name == null)
        {
            return false;
        }

        var userName = _httpContextAccessor.HttpContext.User.Identity.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            return false;
        }

        var userPermissions = await _permissionRepository.GetByUserName(userName, module, action);
        if (userPermissions.Any())
            return true;

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            return true;

        foreach (var roleName in roles)
        {
            var rolePermissions = await _permissionRepository.GetByRoleName(roleName, module, action);
            if (rolePermissions.Any())
                return true;
        }

        return false;
    }
}