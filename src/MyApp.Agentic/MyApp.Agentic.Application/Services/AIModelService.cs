using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Application.Services;

/// <summary>Application service for managing AI model definitions within the Agentic service.</summary>
public class AIModelService : AppServiceBase, IAIModelService
{
    private readonly IAIModelRepository modelRepository;
    private readonly IAIProviderRepository providerRepository;

    /// <summary>Initializes a new instance of the <see cref="AIModelService"/> class.</summary>
    /// <param name="modelRepository">Repository for AI model persistence.</param>
    /// <param name="providerRepository">The provider Repository.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public AIModelService(
        IAIModelRepository modelRepository,
        IAIProviderRepository providerRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<AIModelService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Agentic)
    {
        this.modelRepository = modelRepository;
        this.providerRepository = providerRepository;
    }

    /// <summary>Returns all AI models ordered by provider name then commercial name.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All AI model DTOs.</returns>
    public async Task<IEnumerable<AIModelDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await modelRepository.GetAllAsync();
        return models
            .OrderBy(m => m.Provider?.Name)
            .ThenBy(m => m.CommercialName)
            .Select(MapToDto);
    }

    /// <summary>Returns all AI models belonging to the specified provider.</summary>
    /// <summary>Returns all AI models belonging to the specified provider.</summary>
    /// <param name="providerId">Provider identifier to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI model DTOs for the given provider.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerId"/> is empty.</exception>
    public async Task<IEnumerable<AIModelDto>> ListByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));

        var models = await modelRepository.GetByProviderIdAsync(providerId, cancellationToken);
        return models.Select(MapToDto);
    }

    /// <summary>Retrieves a single AI model by its identifier.</summary>
    /// Gets an item by its unique identifier asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The model DTO, or <see langword="null"/> if not found.</returns>
    public async Task<AIModelDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await modelRepository.GetByIdAsync(id);
        return model is null ? null : MapToDto(model);
    }

    /// <summary>Creates a new AI model, inheriting unspecified parameters from the parent provider.</summary>
    /// Creates a new item asynchronously.
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The created model DTO.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the referenced provider does not exist.</exception>
    public async Task<AIModelDto> CreateAsync(CreateAIModelDto dto, CancellationToken cancellationToken = default)
    {
        var provider = await EnsureProviderExistsAsync(dto.ProviderId);

        var model = new AIModel(
            Guid.NewGuid(),
            dto.ProviderId,
            dto.CommercialName,
            dto.TechnicalName,
            dto.TokenLimit,
            dto.Capabilities,
            dto.DefaultTemperature ?? provider.DefaultTemperature,
            dto.DefaultTopK ?? provider.DefaultTopK,
            dto.DefaultMaxTokens ?? provider.DefaultMaxTokens,
            dto.DefaultEmbeddingDimensions ?? provider.DefaultEmbeddingDimensions,
            dto.DefaultEnableMemory ?? provider.DefaultEnableMemory,
            dto.DefaultEnableRAG ?? provider.DefaultEnableRAG,
            dto.DefaultEmbeddingModelName ?? provider.DefaultEmbeddingModelName,
            dto.DefaultBotType ?? provider.DefaultBotType,
            dto.DefaultSystemPrompt ?? provider.DefaultSystemPrompt);

        await modelRepository.AddAsync(model);
        await SaveChangesAsync(cancellationToken);
        var persisted = await modelRepository.GetByIdAsync(model.Id) ?? model;
        return MapToDto(persisted);
    }

    /// <summary>Updates an existing AI model.</summary>
    /// Updates an existing item asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The updated model DTO.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the model or referenced provider does not exist.</exception>
    public async Task<AIModelDto> UpdateAsync(Guid id, UpdateAIModelDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureProviderExistsAsync(dto.ProviderId);

        var model = await modelRepository.GetByIdAsync(id);
        if (model is null)
            throw new InvalidOperationException($"AI model with ID {id} not found.");

        model.Update(
            dto.ProviderId,
            dto.CommercialName,
            dto.TechnicalName,
            dto.TokenLimit,
            dto.Capabilities,
            dto.DefaultTemperature,
            dto.DefaultTopK,
            dto.DefaultMaxTokens,
            dto.DefaultEmbeddingDimensions,
            dto.DefaultEnableMemory,
            dto.DefaultEnableRAG,
            dto.DefaultEmbeddingModelName,
            dto.DefaultBotType,
            dto.DefaultSystemPrompt);

        await modelRepository.UpdateAsync(model);
        await SaveChangesAsync(cancellationToken);
        var persisted = await modelRepository.GetByIdAsync(model.Id) ?? model;
        return MapToDto(persisted);
    }

    /// <summary>
    /// Deletes an item asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <summary>Deletes the AI model with the specified identifier. Does nothing if the model does not exist.</summary>
    /// <param name="id">Model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await modelRepository.GetByIdAsync(id);
        if (model is null)
            return;

        await modelRepository.DeleteAsync(model);
        await SaveChangesAsync(cancellationToken);
    }

    private async Task<AIProvider> EnsureProviderExistsAsync(Guid providerId)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));

        var provider = await providerRepository.GetByIdAsync(providerId);
        if (provider is null)
            throw new InvalidOperationException($"AI provider with ID {providerId} not found.");

        return provider;
    }

    private static AIModelDto MapToDto(AIModel model) => new(
        model.Id,
        model.ProviderId,
        model.Provider?.Name ?? "N/A",
        model.CommercialName,
        model.TechnicalName,
        model.TokenLimit,
        model.Capabilities,
        model.DefaultTemperature,
        model.DefaultTopK,
        model.DefaultMaxTokens,
        model.DefaultEmbeddingDimensions,
        model.DefaultEnableMemory,
        model.DefaultEnableRAG,
        model.DefaultEmbeddingModelName,
        model.DefaultBotType,
        model.DefaultSystemPrompt);
}
