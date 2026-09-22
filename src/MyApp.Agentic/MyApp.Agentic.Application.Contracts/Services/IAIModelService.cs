using MyApp.Agentic.Application.Contracts.DTOs;

namespace MyApp.Agentic.Application.Contracts.Services;

/// <summary>Service contract for managing AI model definitions.</summary>
public interface IAIModelService
{
    /// <summary>Returns all AI models.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All AI model DTOs.</returns>
    Task<IEnumerable<AIModelDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all AI models belonging to the specified provider.</summary>
    /// <param name="providerId">Provider identifier to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI model DTOs for the given provider.</returns>
    Task<IEnumerable<AIModelDto>> ListByProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single AI model by its identifier.</summary>
    /// <param name="id">Model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model DTO, or <see langword="null"/> if not found.</returns>
    Task<AIModelDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new AI model.</summary>
    /// <param name="dto">Creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created model DTO.</returns>
    Task<AIModelDto> CreateAsync(CreateAIModelDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing AI model.</summary>
    /// <param name="id">Model identifier.</param>
    /// <param name="dto">Update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated model DTO.</returns>
    Task<AIModelDto> UpdateAsync(Guid id, UpdateAIModelDto dto, CancellationToken cancellationToken = default);

    /// <summary>Deletes the AI model with the specified identifier.</summary>
    /// <param name="id">Model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
