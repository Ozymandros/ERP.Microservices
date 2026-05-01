using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

/// <summary>
/// Defines operations for accessing and managing Permission entities.
/// </summary>
public interface IPermissionRepository : IRepository<Permission, Guid>
{
    /// <summary>
    /// Retrieves all permissions assigned to a specific user, either directly or through roles.
    /// </summary>
    Task<IEnumerable<Permission>> GetAllPermissionsByUserId(Guid userId);

    /// <summary>
    /// Retrieves permissions for a specific role by module and action.
    /// </summary>
    Task<IEnumerable<Permission>> GetByRoleName(string roleName, string module, string action);

    /// <summary>
    /// Retrieves permissions for a specific user by module and action.
    /// </summary>
    Task<IEnumerable<Permission>> GetByUserName(string userName, string module, string action);
}
