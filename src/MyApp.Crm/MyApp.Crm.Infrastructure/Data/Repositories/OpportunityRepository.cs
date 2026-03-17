using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

public class OpportunityRepository : Repository<Opportunity, Guid>, IOpportunityRepository
{
    private readonly CrmDbContext _context;

    public OpportunityRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Opportunity?> GetByIdAsync(Guid id)
    {
        return await _context.Opportunities
            .Include(o => o.Notes)
            .Include(o => o.Tags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Opportunity>> ListAsync()
    {
        return await _context.Opportunities.AsNoTracking().ToListAsync();
    }
}

