using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Domain.Security;

namespace MyApp.Agentic.Application.Services;

public class AIProviderService(
    IAIProviderRepository providerRepository,
    ISecretCryptoService secretCryptoService) : IAIProviderService
{
    public async Task<IEnumerable<AIProviderDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var providers = await providerRepository.GetAllAsync();
        return providers
            .OrderBy(p => p.Name)
            .Select(MapToDto);
    }

    public async Task<AIProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.GetByIdAsync(id);
        return provider is null ? null : MapToDto(provider);
    }

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
        return MapToDto(provider);
    }

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
        return MapToDto(provider);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.GetByIdAsync(id);
        if (provider is null)
            return;

        await providerRepository.DeleteAsync(provider);
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
