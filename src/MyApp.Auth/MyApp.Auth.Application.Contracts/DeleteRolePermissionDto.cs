namespace MyApp.Auth.Application.Contracts
{
    /// <summary>
    /// Deletes the role permission dto.
    /// </summary>
    /// <param name="RoleId">The role Id.</param>
    /// <param name="PermissionId">The permission Id.</param>
    public record DeleteRolePermissionDto(Guid RoleId, Guid PermissionId);

    /// <summary>
    /// Deletes the role permissions dto.
    /// </summary>
    /// <param name="RoleId">The role Id.</param>
    /// <param name="PermissionIds">The permission Ids.</param>
    public record DeleteRolePermissionsDto(Guid RoleId, IEnumerable<Guid> PermissionIds);
}