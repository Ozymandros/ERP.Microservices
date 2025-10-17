using Microsoft.AspNetCore.Identity;

public class UserPermission
{
    public Guid Id { get; set; }

    //public Guid UserId { get; set; }
    public IdentityUser User { get; set; } = default!;

    //public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}
