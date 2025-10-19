using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserPermission
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    [ForeignKey("Permission")]
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}
