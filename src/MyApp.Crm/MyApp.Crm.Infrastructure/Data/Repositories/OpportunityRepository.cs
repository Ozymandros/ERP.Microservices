using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Opportunity Repository functionality.
/// </summary>
public class OpportunityRepository : Repository<Opportunity, Guid>, IOpportunityRepository
{
    private readonly CrmDbContext _context;

    /// <summary>base.</summary>
    public OpportunityRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    public override async Task<Opportunity?> GetByIdAsync(Guid id)
    {
        return await _context.Opportunities
            .Include(o => o.Lines)
            .Include(o => o.Notes)
            .Include(o => o.Tags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<Opportunity>> ListAsync()
    {
        return await _context.Opportunities.AsNoTracking().ToListAsync();
    }

    /// <summary>List For Forecast Async.</summary>
    public async Task<List<Opportunity>> ListForForecastAsync(
        string ownerUsername,
        DateOnly? fromExpectedCloseDate,
        DateOnly? toExpectedCloseDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Opportunities.AsNoTracking()
            .Where(o => o.OwnerUsername == ownerUsername);

        if (fromExpectedCloseDate.HasValue)
            query = query.Where(o => o.ExpectedCloseDate.HasValue && o.ExpectedCloseDate.Value >= fromExpectedCloseDate.Value);

        if (toExpectedCloseDate.HasValue)
            query = query.Where(o => o.ExpectedCloseDate.HasValue && o.ExpectedCloseDate.Value <= toExpectedCloseDate.Value);

        return await query.ToListAsync(cancellationToken);
    }
}

