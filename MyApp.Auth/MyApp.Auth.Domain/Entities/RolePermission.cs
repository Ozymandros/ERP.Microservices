using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class RolePermission
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey("Role")]
    public Guid RoleId { get; set; }
    public ApplicationRole Role { get; set; } = default!;

    [ForeignKey("Permission")]
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}
