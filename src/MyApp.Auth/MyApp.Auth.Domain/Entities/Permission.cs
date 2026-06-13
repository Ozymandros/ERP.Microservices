using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Auth.Domain.Entities;

/// <summary>
/// Represents a permission that can be assigned to roles or users.
/// </summary>
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
    /// Compares two permissions by their unique identifiers.
    /// </summary>
    public bool Equals(Permission? x, Permission? y)
        => x?.Id == y?.Id;

    /// <summary>
    /// Returns the hash code for a permission based on its unique identifier.
    /// </summary>
    public int GetHashCode(Permission obj)
        => obj.Id.GetHashCode();
}

