using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Accounts;

public interface IContactRepository : IRepository<Contact, Guid>
{
    Task<IEnumerable<Contact>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}

