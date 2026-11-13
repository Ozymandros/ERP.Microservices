using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Activity Repository functionality.
/// </summary>
public class ActivityRepository : Repository<Activity, Guid>, IActivityRepository
{
    private readonly CrmDbContext _context;

    /// <summary>base.</summary>
    /// <param name="context">The context.</param>
    public ActivityRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public override async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .Include(a => a.Notes)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<Activity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Activities.AsNoTracking().ToListAsync(cancellationToken);
    }
}

