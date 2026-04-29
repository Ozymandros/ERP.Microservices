using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Auth.Application.Contracts;

namespace MyApp.Auth.Application.Contracts.Services;

/// <summary>
/// Defines operations for managing roles and their associated permissions.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Retrieves a role by its unique identifier.
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId);

    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    Task<RoleDto?> GetRoleByNameAsync(string name);

    /// <summary>
    /// Retrieves all available roles.
    /// </summary>
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();

    /// <summary>
    /// Retrieves roles with pagination support.
    /// </summary>
    Task<PaginatedResult<RoleDto>> GetAllRolesPaginatedAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Queries roles using a specification pattern.
    /// </summary>
    Task<PaginatedResult<RoleDto>> QueryRolesAsync(ISpecification<ApplicationRole> spec);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    Task<bool> UpdateRoleAsync(Guid roleId, CreateRoleDto updateRoleDto);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid roleId);

    /// <summary>
    /// Retrieves all users assigned to a specific role.
    /// </summary>
    Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName);

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    Task<bool> AddPermissionToRole(CreateRolePermissionDto createDto);

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    Task<bool> RemovePermissionFromRoleAsync(DeleteRolePermissionDto deleteDto);

    /// <summary>
    /// Determines whether a role has a specific permission.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId);

    /// <summary>
    /// Retrieves all permissions assigned to a role.
    /// </summary>
    Task<IEnumerable<PermissionDto>> GetPermissionsForRoleAsync(Guid roleId);

    /// <summary>
    /// Assigns multiple permissions to a role in a single operation.
    /// </summary>
    Task<bool> AddPermissionsToRole(CreateRolePermissionsDto createDto);

    /// <summary>
    /// Removes multiple permissions from a role in a single operation.
    /// </summary>
    Task<bool> RemovePermissionsFromRoleAsync(DeleteRolePermissionsDto deleteDto);
}
