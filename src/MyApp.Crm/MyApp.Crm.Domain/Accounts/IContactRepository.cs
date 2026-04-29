using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Accounts;

/// <summary>Repository interface for managing Contact entities.</summary>
public interface IContactRepository : IRepository<Contact, Guid>
{
    /// <summary>Gets all contacts for a specific account.</summary>
    Task<IEnumerable<Contact>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}

