using Microsoft.AspNetCore.Identity;

namespace MyApp.Auth.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsExternalLogin { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<IdentityUserRole<Guid>> UserRoles { get; set; } = new List<IdentityUserRole<Guid>>();
    public virtual ICollection<IdentityUserClaim<Guid>> UserClaims { get; set; } = new List<IdentityUserClaim<Guid>>();
    public virtual ICollection<IdentityUserLogin<Guid>> UserLogins { get; set; } = new List<IdentityUserLogin<Guid>>();
}
