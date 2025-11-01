using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

public interface IUserRepository : IRepository<ApplicationUser, Guid>
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByExternalIdAsync(string externalProvider, string externalId);
    Task<IEnumerable<ApplicationUser>> GetByRoleAsync(string roleName);
    Task<bool> EmailExistsAsync(string email);
}
