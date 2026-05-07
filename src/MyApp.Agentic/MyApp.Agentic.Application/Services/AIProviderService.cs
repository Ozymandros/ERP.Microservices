using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.AIProviders;

namespace MyApp.Agentic.Application.Services;

public class AIProviderService(
    IAIProviderRepository providerRepository) : IAIProviderService
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
        var provider = new AIProvider(Guid.NewGuid(), dto.Name, dto.BaseUrl, dto.SecretKeyName);
        await providerRepository.AddAsync(provider);
        return MapToDto(provider);
    }

    public async Task<AIProviderDto> UpdateAsync(Guid id, UpdateAIProviderDto dto, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.GetByIdAsync(id);
        if (provider is null)
            throw new InvalidOperationException($"AI provider with ID {id} not found.");

        provider.Update(dto.Name, dto.BaseUrl, dto.SecretKeyName);
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

    private static AIProviderDto MapToDto(AIProvider provider) => new(
        provider.Id,
        provider.Name,
        provider.BaseUrl,
        provider.SecretKeyName);
}
