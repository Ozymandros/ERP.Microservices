using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Account Repository functionality.
/// </summary>
public sealed class AccountRepository : Repository<Account, Guid>, IAccountRepository
{
    private readonly CrmDbContext _context;

    /// <summary>base.</summary>
    /// <param name="context">The context.</param>
    public AccountRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    /// <param name="id">The id.</param>
    public override async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>Get By Customer Id Async.</summary>
    /// <param name="customerId">The customer Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<Account?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);
    }

    /// <summary>Get By Tax Id Async.</summary>
    /// <param name="taxId">The tax Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<Account?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.TaxId == taxId, cancellationToken);
    }

    /// <summary>List Async.</summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IEnumerable<Account>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }
}

