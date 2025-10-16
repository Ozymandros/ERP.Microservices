using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> GetByNameAsync(string name);
    Task<bool> NameExistsAsync(string name);
    Task<IEnumerable<Role>> GetRolesByUserIdAsync(Guid userId);
}
