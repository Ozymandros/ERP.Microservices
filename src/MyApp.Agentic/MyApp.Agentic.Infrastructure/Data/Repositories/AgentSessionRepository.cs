using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AgentSessionRepository : Repository<AgentSession, Guid>, IAgentSessionRepository
{
    private readonly AgenticDbContext _context;

    public AgentSessionRepository(AgenticDbContext context) : base(context)
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
}