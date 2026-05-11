using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AgentRepository : Repository<Agent, Guid>, IAgentRepository
{
    private readonly AgenticSqlDbContext _context;

    public AgentRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Agent?> GetByIdAsync(Guid id)
    {
        return await _context.Agents
            .Include(a => a.Model)
                .ThenInclude(m => m!.Provider)
            .Include(a => a.Plugins)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Agent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Agents
            .Include(a => a.Model)
                .ThenInclude(m => m!.Provider)
            .Include(a => a.Plugins)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}