using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

public interface IPermissionRepository : IRepository<Permission, Guid>
{
    Task<IEnumerable<Permission>> GetAllPermissionsByUserId(Guid userId);
    Task<IEnumerable<Permission>> GetByRoleName(string roleName, string module, string action);
    Task<IEnumerable<Permission>> GetByUserName(string userName, string module, string action);
}
