namespace MyApp.Auth.Application.Contracts
{
    public record CreateRolePermissionDto(Guid RoleId, Guid PermissionId);

    public record CreateRolePermissionsDto(Guid RoleId, IEnumerable<Guid> PermissionIds);
}