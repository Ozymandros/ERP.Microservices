using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Accounts;

public interface IAccountRepository : IRepository<Account, Guid>
{
    Task<Account?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> ListAsync(CancellationToken cancellationToken = default);
}

