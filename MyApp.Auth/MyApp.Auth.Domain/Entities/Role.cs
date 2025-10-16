using Microsoft.AspNetCore.Identity;

namespace MyApp.Auth.Domain.Entities;

public class Role : IdentityRole<Guid>
{
    //
    // Summary:
    //     Initializes a new instance of Microsoft.AspNetCore.Identity.IdentityRole.
    //
    // Parameters:
    //   roleName:
    //     The role name.
    //
    // Remarks:
    //     The Id property is initialized to form a new GUID string value.
    public Role(string roleName) : base(roleName) { }
    public Role() : base() { }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<IdentityUserRole<Guid>> UserRoles { get; set; } = new List<IdentityUserRole<Guid>>();
    public virtual ICollection<IdentityRoleClaim<Guid>> RoleClaims { get; set; } = new List<IdentityRoleClaim<Guid>>();
}
