namespace MyApp.Auth.Application.Contracts
{
    public class CreateRolePermissionDto
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public CreateRolePermissionDto(Guid roleId, Guid permissionId)
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}