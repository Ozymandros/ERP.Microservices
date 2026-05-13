using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.Sessions;

public interface IAgentSessionRepository : IRepository<AgentSession, Guid>
{
    Task<AgentSession?> GetActiveSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);
    Task<AgentSession?> GetByIdWithAgentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}