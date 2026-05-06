using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AIProviderRepository : Repository<AIProvider, Guid>, IAIProviderRepository
{
    private readonly AgenticDbContext _context;

    public AIProviderRepository(AgenticDbContext context) : base(context)
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