using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.Sessions;

/// <summary>
/// Repository contract for <see cref="AgentSession"/> persistence and retrieval.
/// </summary>
public interface IAgentSessionRepository : IRepository<AgentSession, Guid>
{
    /// <summary>
    /// Gets the most recently active session for the given agent and user, if one exists.
    /// </summary>
    /// <param name="agentId">Identifier of the agent.</param>
    /// <param name="userId">Identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active session, or <see langword="null"/> if none exists.</returns>
    Task<AgentSession?> GetActiveSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a session by identifier, eagerly loading the associated agent with its model and plugins.
    /// </summary>
    /// <param name="id">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session with full agent navigation loaded, or <see langword="null"/> if not found.</returns>
    Task<AgentSession?> GetByIdWithAgentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sessions owned by the specified user, ordered by most recent activity.
    /// </summary>
    /// <param name="userId">Identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sessions belonging to the user.</returns>
    Task<IEnumerable<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}