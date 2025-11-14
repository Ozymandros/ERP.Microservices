namespace MyApp.Auth.Application.Contracts
{
    public record CreateRolePermissionDto(Guid RoleId, Guid PermissionId);
}