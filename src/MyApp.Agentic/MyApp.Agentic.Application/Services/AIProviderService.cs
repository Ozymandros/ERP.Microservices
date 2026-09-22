using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Security;

namespace MyApp.Agentic.Application.Services;

/// <summary>Application service for managing AI provider configurations, including encrypted API key handling.</summary>
public class AIProviderService : AppServiceBase, IAIProviderService
{
    private readonly IAIProviderRepository providerRepository;
    private readonly ISecretCryptoService secretCryptoService;

    /// <summary>
    /// Initializes a new instance of the AIProviderService class.
    /// </summary>
    /// <param name="providerRepository">The provider Repository.</param>
    /// <param name="secretCryptoService">The secret Crypto Service.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public AIProviderService(
        IAIProviderRepository providerRepository,
        ISecretCryptoService secretCryptoService,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<AIProviderService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Agentic)
    {
        this.providerRepository = providerRepository;
        this.secretCryptoService = secretCryptoService;
    }

    /// <summary>
    /// Lists items asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<AIProviderDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var providers = await providerRepository.GetAllAsync();
        return providers
            .OrderBy(p => p.Name)
            .Select(MapToDto);
    }

    /// <summary>
    /// Gets an item by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<AIProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.GetByIdAsync(id);
        return provider is null ? null : MapToDto(provider);
    }

    /// <summary>
    /// Creates a new item asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<AIProviderDto> CreateAsync(CreateAIProviderDto dto, CancellationToken cancellationToken = default)
    {
        var encryptedApiKey = string.IsNullOrWhiteSpace(dto.ApiKey)
            ? null
            : secretCryptoService.Encrypt(dto.ApiKey);

        var provider = new AIProvider(
            Guid.NewGuid(),
            dto.Name,
            dto.BaseUrl,
            encryptedApiKey,
            dto.DefaultTemperature,
            dto.DefaultTopK,
            dto.DefaultMaxTokens,
            dto.DefaultEmbeddingDimensions,
            dto.DefaultEnableMemory,
            dto.DefaultEnableRAG,
            dto.DefaultEmbeddingModelName,
            dto.DefaultBotType,
            dto.DefaultSystemPrompt);
        await providerRepository.AddAsync(provider);
        await SaveChangesAsync(cancellationToken);
        return MapToDto(provider);
    }

    /// <summary>
    /// Updates an existing item asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<AIProviderDto> UpdateAsync(Guid id, UpdateAIProviderDto dto, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.GetByIdAsync(id);
        if (provider is null)
            throw new InvalidOperationException($"AI provider with ID {id} not found.");

        var encryptedApiKey = provider.EncryptedApiKey;
        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            var requestedApiKey = dto.ApiKey.Trim();
            // Only re-encrypt if the incoming value differs from the stored encrypted value
            if (!string.Equals(requestedApiKey, provider.EncryptedApiKey, StringComparison.Ordinal))
                encryptedApiKey = secretCryptoService.Encrypt(requestedApiKey);
        }

        provider.Update(
            dto.Name,
            dto.BaseUrl,
            encryptedApiKey,
            dto.DefaultTemperature,
            dto.DefaultTopK,
            dto.DefaultMaxTokens,
            dto.DefaultEmbeddingDimensions,
            dto.DefaultEnableMemory,
            dto.DefaultEnableRAG,
            dto.DefaultEmbeddingModelName,
            dto.DefaultBotType,
            dto.DefaultSystemPrompt);
        await providerRepository.UpdateAsync(provider);
        await SaveChangesAsync(cancellationToken);
        return MapToDto(provider);
    }

    /// <summary>
    /// Deletes an item asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.GetByIdAsync(id);
        if (provider is null)
            return;

        await providerRepository.DeleteAsync(provider);
        await SaveChangesAsync(cancellationToken);
    }

    private AIProviderDto MapToDto(AIProvider provider)
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(provider.EncryptedApiKey);
        var apiKey = hasApiKey ? provider.EncryptedApiKey : null;

        return new AIProviderDto(
            provider.Id,
            provider.Name,
            provider.BaseUrl,
            apiKey,
            hasApiKey,
            provider.DefaultTemperature,
            provider.DefaultTopK,
            provider.DefaultMaxTokens,
            provider.DefaultEmbeddingDimensions,
            provider.DefaultEnableMemory,
            provider.DefaultEnableRAG,
            provider.DefaultEmbeddingModelName,
            provider.DefaultBotType,
            provider.DefaultSystemPrompt);
    }
}
