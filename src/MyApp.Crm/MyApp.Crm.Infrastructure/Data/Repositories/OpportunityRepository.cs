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
    /// <param name="context">The context.</param>
    public OpportunityRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    /// <param name="id">The id.</param>
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
    /// <param name="ownerUsername">The owner Username.</param>
    /// <param name="fromExpectedCloseDate">The from Expected Close Date.</param>
    /// <param name="toExpectedCloseDate">The to Expected Close Date.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
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

