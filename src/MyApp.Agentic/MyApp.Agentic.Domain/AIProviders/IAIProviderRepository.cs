using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.AIProviders;

public interface IAIProviderRepository : IRepository<AIProvider, Guid>
{
}