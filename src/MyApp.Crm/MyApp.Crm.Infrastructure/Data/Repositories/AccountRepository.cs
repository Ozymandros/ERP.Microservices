using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

public sealed class AccountRepository : Repository<Account, Guid>, IAccountRepository
{
    private readonly CrmDbContext _context;

    public AccountRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Account?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);
    }

    public async Task<IEnumerable<Account>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }
}

