using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.AIModels;

public interface IAIModelRepository : IRepository<AIModel, Guid>
{
    Task<IEnumerable<AIModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
}