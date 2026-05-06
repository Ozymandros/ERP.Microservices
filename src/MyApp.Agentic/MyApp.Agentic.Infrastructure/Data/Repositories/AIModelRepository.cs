using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AIModelRepository : Repository<AIModel, Guid>, IAIModelRepository
{
    private readonly AgenticDbContext _context;

    public AIModelRepository(AgenticDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<AIModel?> GetByIdAsync(Guid id)
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}