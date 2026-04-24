using MyApp.Auth.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents the assignment of a permission to a role.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Gets or sets the unique identifier for this role-permission assignment.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the role.
    /// </summary>
    [ForeignKey("Role")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the role to which the permission is assigned.
    /// </summary>
    public ApplicationRole Role { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unique identifier of the permission.
    /// </summary>
    [ForeignKey("Permission")]
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Gets or sets the permission assigned to the role.
    /// </summary>
    public Permission Permission { get; set; } = default!;
}
