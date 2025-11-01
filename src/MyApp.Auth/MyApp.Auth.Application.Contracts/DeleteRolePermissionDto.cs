
namespace MyApp.Auth.Application.Contracts
{
    public class DeleteRolePermissionDto
    {
        private Guid roleId;
        private Guid permissionId;

        public DeleteRolePermissionDto(Guid roleId, Guid permissionId)
        {
            this.roleId = roleId;
            this.permissionId = permissionId;
        }
    }
}