namespace MyApp.Auth.Application.Contracts
{
    /// <summary>
    /// Data transfer object for removing a single permission from a role.
    /// </summary>
    public record DeleteRolePermissionDto(Guid RoleId, Guid PermissionId);

    /// <summary>
    /// Data transfer object for removing multiple permissions from a role.
    /// </summary>
    public record DeleteRolePermissionsDto(Guid RoleId, IEnumerable<Guid> PermissionIds);
}