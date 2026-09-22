using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.AIModels;

/// <summary>
/// Repository contract for <see cref="AIModel"/> persistence and retrieval.
/// </summary>
public interface IAIModelRepository : IRepository<AIModel, Guid>
{
    /// <summary>
    /// Retrieves all AI models belonging to the specified provider.
    /// </summary>
    /// <param name="providerId">Identifier of the owning AI provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All models registered under the given provider.</returns>
    Task<IEnumerable<AIModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
}