using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AgentSessionRepository : Repository<AgentSession, Guid>, IAgentSessionRepository
{
    private readonly AgenticSqlDbContext _context;

    public AgentSessionRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<AgentSession?> GetByIdAsync(Guid id)
    {
        return await _context.AgentSessions
            .Include(s => s.Agent)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<AgentSession?> GetActiveSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessions
            .Where(s => s.AgentId == agentId && s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AgentSession?> GetByIdWithAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessions
            .Include(s => s.Agent)
                .ThenInclude(a => a!.Model)
                    .ThenInclude(m => m!.Provider)
            .Include(s => s.Agent)
                .ThenInclude(a => a!.Plugins)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessions
            .Include(s => s.Agent)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .ToListAsync(cancellationToken);
    }
}