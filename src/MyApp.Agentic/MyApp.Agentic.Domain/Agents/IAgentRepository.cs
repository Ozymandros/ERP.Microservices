using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.Agents;

/// <summary>
/// Repository contract for <see cref="Agent"/> persistence and retrieval.
/// </summary>
public interface IAgentRepository : IRepository<Agent, Guid>
{
    /// <summary>
    /// Retrieves an agent by identifier, eagerly loading the model, provider, and plugins.
    /// </summary>
    /// <param name="id">Identifier of the agent to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent with full navigation properties loaded, or <see langword="null"/> if not found.</returns>
    Task<Agent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}