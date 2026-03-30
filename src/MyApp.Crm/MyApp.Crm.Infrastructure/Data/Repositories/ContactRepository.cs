using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

public sealed class ContactRepository : Repository<Contact, Guid>, IContactRepository
{
    private readonly CrmDbContext _context;

    public ContactRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Contact>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Contacts
            .AsNoTracking()
            .Where(c => c.AccountId == accountId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.FullName)
            .ToListAsync(cancellationToken);
    }
}

