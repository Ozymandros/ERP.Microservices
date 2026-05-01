using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Auth.Domain.Entities;

/// <summary>
/// Represents an application user with authentication and audit tracking capabilities.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity<Guid>
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user logged in through an external provider.
    /// </summary>
    public bool IsExternalLogin { get; set; }

    /// <summary>
    /// Gets or sets the external authentication provider name (e.g., "Google", "Facebook").
    /// </summary>
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Gets or sets the external provider's unique identifier for this user.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created this account.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the user account was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated this account.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection of refresh tokens issued to this user.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Gets or sets the collection of roles assigned to this user.
    /// </summary>
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

    /// <summary>
    /// Gets or sets the collection of claims associated with this user.
    /// </summary>
    public virtual ICollection<IdentityUserClaim<Guid>> UserClaims { get; set; } = new List<IdentityUserClaim<Guid>>();

    /// <summary>
    /// Gets or sets the collection of external login associations for this user.
    /// </summary>
    public virtual ICollection<IdentityUserLogin<Guid>> UserLogins { get; set; } = new List<IdentityUserLogin<Guid>>();
}
