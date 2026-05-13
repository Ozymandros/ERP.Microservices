using MyApp.Agentic.Application.Contracts.DTOs;

namespace MyApp.Agentic.Application.Contracts.Services;

public interface IAIProviderService
{
    Task<IEnumerable<AIProviderDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AIProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AIProviderDto> CreateAsync(CreateAIProviderDto dto, CancellationToken cancellationToken = default);
    Task<AIProviderDto> UpdateAsync(Guid id, UpdateAIProviderDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
