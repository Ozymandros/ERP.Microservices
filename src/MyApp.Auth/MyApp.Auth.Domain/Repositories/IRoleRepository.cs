using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

/// <summary>
/// Defines operations for accessing and managing ApplicationRole entities.
/// </summary>
public interface IRoleRepository : IRepository<ApplicationRole, Guid>
{
    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    Task<ApplicationRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a role with the specified name exists.
    /// </summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all roles assigned to a specific user.
    /// </summary>
    Task<IEnumerable<ApplicationRole>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all permissions assigned to a specific role.
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a role has a specific permission.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}
