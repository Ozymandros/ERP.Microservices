using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Application.Tests;

public class AIModelServiceTests
{
    [Fact]
    public async Task ListByProviderAsync_ReturnsOnlyProviderModels()
    {
        var providerId = Guid.NewGuid();
        var provider = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "sk-test-key");
        var models = new List<AIModel>
        {
            new(Guid.NewGuid(), providerId, "GPT-5", "gpt-5", 8192, "chat"),
            new(Guid.NewGuid(), providerId, "GPT-5 Mini", "gpt-5-mini", 8192, "chat")
        };

        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        modelRepo.Setup(r => r.GetByProviderIdAsync(providerId, It.IsAny<CancellationToken>())).ReturnsAsync(models);
        providerRepo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync(provider);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIModelService(modelRepo.Object, providerRepo.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIModelService>>());

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

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIModelService(modelRepo.Object, providerRepo.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIModelService>>());
        var dto = new CreateAIModelDto(providerId, "GPT-5", "gpt-5", 8192, "chat");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenDefaultsOmitted_InheritsProviderDefaults()
    {
        var providerId = Guid.NewGuid();
        var provider = new AIProvider(
            providerId,
            "OpenAI",
            "https://api.openai.com/v1",
            "sk-test-key",
            defaultTemperature: 1.1,
            defaultTopK: 7,
            defaultMaxTokens: 4096,
            defaultEmbeddingDimensions: 3072,
            defaultEnableMemory: false,
            defaultEnableRAG: false,
            defaultEmbeddingModelName: "text-embedding-3-large",
            defaultBotType: Domain.Agents.BotType.Agent,
            defaultSystemPrompt: "Provider default prompt");

        AIModel? captured = null;
        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        providerRepo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync(provider);
        modelRepo.Setup(r => r.AddAsync(It.IsAny<AIModel>()))
            .Callback<AIModel>(m => captured = m)
            .ReturnsAsync((AIModel m) => m);
        modelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AIModel?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIModelService(modelRepo.Object, providerRepo.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIModelService>>());
        var dto = new CreateAIModelDto(providerId, "GPT-5", "gpt-5", 8192, "chat");

        await sut.CreateAsync(dto);

        Assert.NotNull(captured);
        Assert.Equal(1.1, captured!.DefaultTemperature);
        Assert.Equal(7, captured.DefaultTopK);
        Assert.Equal(4096, captured.DefaultMaxTokens);
        Assert.Equal(3072, captured.DefaultEmbeddingDimensions);
        Assert.False(captured.DefaultEnableMemory);
        Assert.False(captured.DefaultEnableRAG);
        Assert.Equal("text-embedding-3-large", captured.DefaultEmbeddingModelName);
        Assert.Equal(Domain.Agents.BotType.Agent, captured.DefaultBotType);
        Assert.Equal("Provider default prompt", captured.DefaultSystemPrompt);
    }

    [Fact]
    public async Task CreateAsync_WhenDefaultsProvided_UsesExplicitOverrides()
    {
        var providerId = Guid.NewGuid();
        var provider = new AIProvider(
            providerId,
            "OpenAI",
            "https://api.openai.com/v1",
            "sk-test-key",
            defaultTemperature: 0.2,
            defaultTopK: 2,
            defaultMaxTokens: 2048,
            defaultEmbeddingDimensions: 1536,
            defaultEnableMemory: false,
            defaultEnableRAG: false,
            defaultEmbeddingModelName: "provider-embedding",
            defaultBotType: Domain.Agents.BotType.Chat,
            defaultSystemPrompt: "Provider prompt");

        AIModel? captured = null;
        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        providerRepo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync(provider);
        modelRepo.Setup(r => r.AddAsync(It.IsAny<AIModel>()))
            .Callback<AIModel>(m => captured = m)
            .ReturnsAsync((AIModel m) => m);
        modelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AIModel?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIModelService(modelRepo.Object, providerRepo.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIModelService>>());
        var dto = new CreateAIModelDto(
            providerId,
            "GPT-5",
            "gpt-5",
            8192,
            "chat",
            DefaultTemperature: 0.95,
            DefaultTopK: 11,
            DefaultMaxTokens: 12000,
            DefaultEmbeddingDimensions: 4096,
            DefaultEnableMemory: true,
            DefaultEnableRAG: true,
            DefaultEmbeddingModelName: "model-embedding",
            DefaultBotType: Domain.Agents.BotType.Agent,
            DefaultSystemPrompt: "Model prompt");

        await sut.CreateAsync(dto);

        Assert.NotNull(captured);
        Assert.Equal(0.95, captured!.DefaultTemperature);
        Assert.Equal(11, captured.DefaultTopK);
        Assert.Equal(12000, captured.DefaultMaxTokens);
        Assert.Equal(4096, captured.DefaultEmbeddingDimensions);
        Assert.True(captured.DefaultEnableMemory);
        Assert.True(captured.DefaultEnableRAG);
        Assert.Equal("model-embedding", captured.DefaultEmbeddingModelName);
        Assert.Equal(Domain.Agents.BotType.Agent, captured.DefaultBotType);
        Assert.Equal("Model prompt", captured.DefaultSystemPrompt);
    }
}
