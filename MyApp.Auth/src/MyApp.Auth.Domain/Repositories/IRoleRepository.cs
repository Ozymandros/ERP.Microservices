using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

public interface IRoleRepository : IRepository<ApplicationRole, Guid>
{
    Task<ApplicationRole?> GetByNameAsync(string name);
    Task<bool> NameExistsAsync(string name);
    Task<IEnumerable<ApplicationRole>> GetRolesByUserIdAsync(Guid userId);
    Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(Guid roleId);
}
