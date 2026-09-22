using MyApp.Agentic.Application.Contracts.DTOs;

namespace MyApp.Agentic.Application.Contracts.Services;

/// <summary>Service contract for managing AI provider configurations.</summary>
public interface IAIProviderService
{
    /// <summary>Returns all configured AI providers.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All AI provider DTOs.</returns>
    Task<IEnumerable<AIProviderDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single AI provider by its identifier.</summary>
    /// <param name="id">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider DTO, or <see langword="null"/> if not found.</returns>
    Task<AIProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new AI provider configuration.</summary>
    /// <param name="dto">Creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created provider DTO.</returns>
    Task<AIProviderDto> CreateAsync(CreateAIProviderDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing AI provider configuration.</summary>
    /// <param name="id">Provider identifier.</param>
    /// <param name="dto">Update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated provider DTO.</returns>
    Task<AIProviderDto> UpdateAsync(Guid id, UpdateAIProviderDto dto, CancellationToken cancellationToken = default);

    /// <summary>Deletes the AI provider with the specified identifier.</summary>
    /// <param name="id">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
