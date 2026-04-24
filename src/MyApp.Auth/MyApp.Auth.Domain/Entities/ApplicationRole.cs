using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Auth.Domain.Entities;

/// <summary>
/// Represents an application role with audit tracking and permission management.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>, IAuditableEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of ApplicationRole with the specified role name.
    /// </summary>
    public ApplicationRole(string roleName) : base(roleName) { }

    /// <summary>
    /// Initializes a new instance of ApplicationRole with a new GUID identifier.
    /// </summary>
    public ApplicationRole() : base() { }

    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the role.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the role was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the role.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection of user-role assignments for this role.
    /// </summary>
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

    /// <summary>
    /// Gets or sets the collection of role claims associated with this role.
    /// </summary>
    public virtual ICollection<IdentityRoleClaim<Guid>> RoleClaims { get; set; } = new List<IdentityRoleClaim<Guid>>();

    /// <summary>
    /// Gets or sets the collection of permissions assigned to this role.
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
