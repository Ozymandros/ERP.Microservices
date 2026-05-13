using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.Agents;

public interface IAgentRepository : IRepository<Agent, Guid>
{
    Task<Agent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}