using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Auth.Domain.Entities;

/// <summary>
/// Permission.
/// </summary>
/// <param name="id">The id.</param>
public class Permission(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>
    /// Gets or sets the module or feature name associated with this permission.
    /// </summary>
    public required string Module { get; set; }

    /// <summary>
    /// Gets or sets the action name for this permission.
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the optional description of this permission.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Provides equality comparison for Permission objects based on their unique identifier.
/// </summary>
public class PermissionComparer : IEqualityComparer<Permission>
{
    /// <summary>
    /// Equals.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public bool Equals(Permission? x, Permission? y)
        => x?.Id == y?.Id;

    /// <summary>
    /// Gets the hash code.
    /// </summary>
    /// <param name="obj">The obj.</param>
    public int GetHashCode(Permission obj)
        => obj.Id.GetHashCode();
}

