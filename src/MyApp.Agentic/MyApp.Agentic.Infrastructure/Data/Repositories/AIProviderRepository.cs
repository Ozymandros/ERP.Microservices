using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AIProviderRepository : Repository<AIProvider, Guid>, IAIProviderRepository
{
    private readonly AgenticSqlDbContext _context;

    public AIProviderRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<AIProvider?> GetByIdAsync(Guid id)
    {
        return await _context.AIProviders
            .Include(p => p.Models)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}