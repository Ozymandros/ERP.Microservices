using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AgentSessionRepository : Repository<AgentSession, Guid>, IAgentSessionRepository
{
    private readonly AgenticSqlDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AgentSessionRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public AgentSessionRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets an item by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public override async Task<AgentSession?> GetByIdAsync(Guid id)
    {
        return await _context.AgentSessions
            .Include(s => s.Agent)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <summary>
    /// Gets the active session asynchronously.
    /// </summary>
    /// <param name="agentId">The agent Id.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<AgentSession?> GetActiveSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessions
            .Where(s => s.AgentId == agentId && s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the id with agent asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
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

    /// <summary>
    /// Gets the user id asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSessions
            .Include(s => s.Agent)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .ToListAsync(cancellationToken);
    }
}