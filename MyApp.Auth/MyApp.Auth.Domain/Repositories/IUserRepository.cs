using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByExternalIdAsync(string externalProvider, string externalId);
    Task<IEnumerable<User>> GetByRoleAsync(string roleName);
    Task<bool> EmailExistsAsync(string email);
}
