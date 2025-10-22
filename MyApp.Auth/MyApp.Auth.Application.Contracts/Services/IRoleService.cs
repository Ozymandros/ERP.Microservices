using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Contracts.Services;

public interface IRoleService
{
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId);
    Task<RoleDto?> GetRoleByNameAsync(string name);
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto);
    Task<bool> UpdateRoleAsync(Guid roleId, CreateRoleDto updateRoleDto);
    Task<bool> DeleteRoleAsync(Guid roleId);
    Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName);
    Task<bool> AddPermissionToRole(CreateRolePermissionDto createDto);
    Task<bool> RemovePermissionFromRoleAsync(DeleteRolePermissionDto deleteDto);
    Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId);

    Task<IEnumerable<PermissionDto>> GetPermissionsForRoleAsync(Guid roleId);
}
