using MyApp.Agentic.Application.Contracts.DTOs;

namespace MyApp.Agentic.Application.Contracts.Services;

public interface IAIModelService
{
    Task<IEnumerable<AIModelDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AIModelDto>> ListByProviderAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<AIModelDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AIModelDto> CreateAsync(CreateAIModelDto dto, CancellationToken cancellationToken = default);
    Task<AIModelDto> UpdateAsync(Guid id, UpdateAIModelDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
