using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Lead Repository functionality.
/// </summary>
public class LeadRepository : Repository<Lead, Guid>, ILeadRepository
{
    private readonly CrmDbContext _context;

    /// <summary>base.</summary>
    public LeadRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    public override async Task<Lead?> GetByIdAsync(Guid id)
    {
        return await _context.Leads
            .Include(l => l.Notes)
            .Include(l => l.Tags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <summary>Get By Id For Update Async.</summary>
    public async Task<Lead?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<Lead>> ListAsync()
    {
        return await _context.Leads
            .AsNoTracking()
            .ToListAsync();
    }
}

