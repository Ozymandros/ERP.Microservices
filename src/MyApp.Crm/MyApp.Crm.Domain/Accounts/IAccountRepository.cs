using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Accounts;

/// <summary>Repository interface for managing Account entities.</summary>
public interface IAccountRepository : IRepository<Account, Guid>
{
    /// <summary>Gets an account by its customer ID.</summary>
    Task<Account?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    /// <summary>Gets an account by its tax ID.</summary>
    Task<Account?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default);
    /// <summary>Gets all accounts.</summary>
    Task<IEnumerable<Account>> ListAsync(CancellationToken cancellationToken = default);
}

