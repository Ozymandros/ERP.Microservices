using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Auth.API.Permissions;

public class PermissionChecker : IPermissionChecker
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionChecker(
        UserManager<ApplicationUser> userManager,
        IPermissionRepository permissionRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _permissionRepository = permissionRepository;
        _httpContextAccessor = httpContextAccessor;
    }

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