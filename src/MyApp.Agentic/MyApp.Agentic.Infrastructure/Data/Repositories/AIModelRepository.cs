using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AIModelRepository : Repository<AIModel, Guid>, IAIModelRepository
{
    private readonly AgenticSqlDbContext _context;

    public AIModelRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<AIModel?> GetByIdAsync(Guid id)
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public override async Task<IEnumerable<AIModel>> GetAllAsync()
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .OrderBy(m => m.Provider!.Name)
            .ThenBy(m => m.CommercialName)
            .ToListAsync();
    }

    public async Task<IEnumerable<AIModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .Where(m => m.ProviderId == providerId)
            .OrderBy(m => m.CommercialName)
            .ToListAsync(cancellationToken);
    }
}