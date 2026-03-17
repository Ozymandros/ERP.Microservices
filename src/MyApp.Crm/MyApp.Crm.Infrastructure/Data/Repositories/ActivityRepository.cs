using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Crm.Infrastructure.Data.Repositories;

public class ActivityRepository : Repository<Activity, Guid>, IActivityRepository
{
    private readonly CrmDbContext _context;

    public ActivityRepository(CrmDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Activity?> GetByIdAsync(Guid id)
    {
        return await _context.Activities
            .Include(a => a.Notes)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Activity>> ListAsync()
    {
        return await _context.Activities.AsNoTracking().ToListAsync();
    }
}

