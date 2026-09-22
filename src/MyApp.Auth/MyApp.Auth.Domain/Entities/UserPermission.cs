using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Auth.Domain.Entities;

/// <summary>
/// Represents the assignment of a permission directly to a user.
/// </summary>
public class UserPermission
{
    /// <summary>
    /// Gets or sets the unique identifier for this user-permission assignment.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    [ForeignKey("User")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user to which the permission is assigned.
    /// </summary>
    public ApplicationUser User { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unique identifier of the permission.
    /// </summary>
    [ForeignKey("Permission")]
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Gets or sets the permission assigned to the user.
    /// </summary>
    public Permission Permission { get; set; } = default!;
}
