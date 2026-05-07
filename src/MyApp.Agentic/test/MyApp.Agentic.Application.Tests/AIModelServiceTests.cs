using Moq;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;

namespace MyApp.Agentic.Application.Tests;

public class AIModelServiceTests
{
    [Fact]
    public async Task ListByProviderAsync_ReturnsOnlyProviderModels()
    {
        var providerId = Guid.NewGuid();
        var provider = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "OpenAI__ApiKey");
        var models = new List<AIModel>
        {
            new(Guid.NewGuid(), providerId, "GPT-5", "gpt-5", 8192, "chat"),
            new(Guid.NewGuid(), providerId, "GPT-5 Mini", "gpt-5-mini", 8192, "chat")
        };

        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        modelRepo.Setup(r => r.GetByProviderIdAsync(providerId, It.IsAny<CancellationToken>())).ReturnsAsync(models);
        providerRepo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync(provider);

        var sut = new AIModelService(modelRepo.Object, providerRepo.Object);

        var result = await sut.ListByProviderAsync(providerId);

        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.Equal(providerId, m.ProviderId));
    }

    [Fact]
    public async Task CreateAsync_WithMissingProvider_Throws()
    {
        var providerId = Guid.NewGuid();
        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        providerRepo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync((AIProvider?)null);

        var sut = new AIModelService(modelRepo.Object, providerRepo.Object);
        var dto = new CreateAIModelDto(providerId, "GPT-5", "gpt-5", 8192, "chat");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(dto));
    }
}
