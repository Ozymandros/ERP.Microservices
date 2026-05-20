using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Agentic.Application.Services;

public class AIModelService : AppServiceBase, IAIModelService
{
    private readonly IAIModelRepository modelRepository;
    private readonly IAIProviderRepository providerRepository;

    public AIModelService(
        IAIModelRepository modelRepository,
        IAIProviderRepository providerRepository,
        IServiceInvoker serviceInvoker,
        ILogger<AIModelService> logger)
        : base(serviceInvoker, logger)
    {
        this.modelRepository = modelRepository;
        this.providerRepository = providerRepository;
    }

    public async Task<IEnumerable<AIModelDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await modelRepository.GetAllAsync();
        return models
            .OrderBy(m => m.Provider?.Name)
            .ThenBy(m => m.CommercialName)
            .Select(MapToDto);
    }

    public async Task<IEnumerable<AIModelDto>> ListByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));

        var models = await modelRepository.GetByProviderIdAsync(providerId, cancellationToken);
        return models.Select(MapToDto);
    }

    public async Task<AIModelDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await modelRepository.GetByIdAsync(id);
        return model is null ? null : MapToDto(model);
    }

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
        var persisted = await modelRepository.GetByIdAsync(model.Id) ?? model;
        return MapToDto(persisted);
    }

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
        var persisted = await modelRepository.GetByIdAsync(model.Id) ?? model;
        return MapToDto(persisted);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await modelRepository.GetByIdAsync(id);
        if (model is null)
            return;

        await modelRepository.DeleteAsync(model);
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
