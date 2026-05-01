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
    public ActivityRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    public override async Task<Activity?> GetByIdAsync(Guid id)
    {
        return await _context.Activities
            .Include(a => a.Notes)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<Activity>> ListAsync()
    {
        return await _context.Activities.AsNoTracking().ToListAsync();
    }
}

