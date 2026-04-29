using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

/// <summary>
/// Defines operations for accessing and managing ApplicationUser entities.
/// </summary>
public interface IUserRepository : IRepository<ApplicationUser, Guid>
{
    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    Task<ApplicationUser?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by their external provider identifier.
    /// </summary>
    Task<ApplicationUser?> GetByExternalIdAsync(string externalProvider, string externalId);

    /// <summary>
    /// Retrieves all users assigned to a specific role.
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetByRoleAsync(string roleName);

    /// <summary>
    /// Determines whether a user with the specified email address exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);
}
