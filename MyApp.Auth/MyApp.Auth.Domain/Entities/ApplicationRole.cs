using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Auth.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>, IAuditableEntity<Guid>
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
    public ApplicationRole(string roleName) : base(roleName) { }
    public ApplicationRole() : base() { }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<IdentityUserRole<Guid>> UserRoles { get; set; } = new List<IdentityUserRole<Guid>>();
    public virtual ICollection<IdentityRoleClaim<Guid>> RoleClaims { get; set; } = new List<IdentityRoleClaim<Guid>>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
