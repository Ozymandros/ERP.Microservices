namespace MyApp.Auth.Application.Contracts
{
    /// <summary>
    /// Creates a role permission dto.
    /// </summary>
    /// <param name="RoleId">The role Id.</param>
    /// <param name="PermissionId">The permission Id.</param>
    public record CreateRolePermissionDto(Guid RoleId, Guid PermissionId);

    /// <summary>
    /// Creates a role permissions dto.
    /// </summary>
    /// <param name="RoleId">The role Id.</param>
    /// <param name="PermissionIds">The permission Ids.</param>
    public record CreateRolePermissionsDto(Guid RoleId, IEnumerable<Guid> PermissionIds);
}