namespace MyApp.Auth.Application.Contracts
{
    /// <summary>
    /// Data transfer object for assigning a single permission to a role.
    /// </summary>
    public record CreateRolePermissionDto(Guid RoleId, Guid PermissionId);

    /// <summary>
    /// Data transfer object for assigning multiple permissions to a role.
    /// </summary>
    public record CreateRolePermissionsDto(Guid RoleId, IEnumerable<Guid> PermissionIds);
}