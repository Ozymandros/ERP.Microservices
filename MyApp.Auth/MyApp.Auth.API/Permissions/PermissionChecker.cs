using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;

public class PermissionService : IPermissionService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(UserManager<ApplicationUser> userManager, IPermissionRepository permissionRepository)
    {
        _userManager = userManager;
        _permissionRepository = permissionRepository;
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

    public async Task<bool> HasPermissionAsync(string userName, string module, string action)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        var userPermissions = await _permissionRepository.GetByUserName(userName, module, action);
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
}