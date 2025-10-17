using Microsoft.AspNetCore.Identity;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public IdentityRole Role { get; set; } = default!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}
