namespace MyApp.Auth.Application.Contracts
{
    public record DeleteRolePermissionDto(Guid RoleId, Guid PermissionId);

    public record DeleteRolePermissionsDto(Guid RoleId, IEnumerable<Guid> PermissionIds);
}