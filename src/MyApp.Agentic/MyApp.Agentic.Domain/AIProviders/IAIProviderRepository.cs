using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.AIProviders;

/// <summary>
/// Repository contract for <see cref="AIProvider"/> persistence and retrieval.
/// </summary>
public interface IAIProviderRepository : IRepository<AIProvider, Guid>
{
}