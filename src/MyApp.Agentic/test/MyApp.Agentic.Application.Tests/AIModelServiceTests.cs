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

    [Fact]
    public async Task ListAsync_ReturnsOrderedModels()
    {
        var providerA = new AIProvider(Guid.NewGuid(), "Beta", "https://beta.example.com", "key");
        var providerB = new AIProvider(Guid.NewGuid(), "Alpha", "https://alpha.example.com", "key");
        var models = new List<AIModel>
        {
            new(Guid.NewGuid(), providerA.Id, "Zeta", "zeta", 8192, "chat"),
            new(Guid.NewGuid(), providerB.Id, "Alpha Model", "alpha", 8192, "chat"),
            new(Guid.NewGuid(), providerB.Id, "Beta Model", "beta", 8192, "chat")
        };
        models[0].SetProviderForTest(providerA);
        models[1].SetProviderForTest(providerB);
        models[2].SetProviderForTest(providerB);

        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        modelRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(models);

        var sut = CreateService(modelRepo, providerRepo);
        var result = (await sut.ListAsync(CancellationToken.None)).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].ProviderName);
        Assert.Equal("Alpha Model", result[0].CommercialName);
        Assert.Equal("Beta Model", result[1].CommercialName);
        Assert.Equal("Beta", result[2].ProviderName);
    }

    [Fact]
    public async Task ListByProviderAsync_WithEmptyProviderId_Throws()
    {
        var sut = CreateService(new Mock<IAIModelRepository>(), new Mock<IAIProviderRepository>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ListByProviderAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var model = new AIModel(modelId, providerId, "GPT-5", "gpt-5", 8192, "chat");
        var provider = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "key");
        model.SetProviderForTest(provider);

        var modelRepo = new Mock<IAIModelRepository>();
        modelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync(model);

        var sut = CreateService(modelRepo, new Mock<IAIProviderRepository>());
        var result = await sut.GetByIdAsync(modelId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(modelId, result!.Id);
        Assert.Equal("GPT-5", result.CommercialName);
        Assert.Equal("OpenAI", result.ProviderName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var modelRepo = new Mock<IAIModelRepository>();
        modelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIModel?)null);

        var sut = CreateService(modelRepo, new Mock<IAIProviderRepository>());
        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesModelFields()
    {
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var provider = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "key");
        var model = new AIModel(modelId, providerId, "Old", "old", 4096, "chat");

        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        providerRepo.Setup(r => r.GetByIdAsync(providerId, It.IsAny<CancellationToken>())).ReturnsAsync(provider);
        modelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync(model);
        modelRepo.Setup(r => r.UpdateAsync(model)).ReturnsAsync(model);

        var sut = CreateService(modelRepo, providerRepo);
        var dto = new UpdateAIModelDto(
            providerId,
            "New Name",
            "new-tech",
            16000,
            "chat,vision",
            DefaultTemperature: 0.3,
            DefaultTopK: 5,
            DefaultMaxTokens: 8000,
            DefaultEmbeddingDimensions: 2048,
            DefaultEnableMemory: false,
            DefaultEnableRAG: false,
            DefaultEmbeddingModelName: "embed-new",
            DefaultBotType: Domain.Agents.BotType.Agent,
            DefaultSystemPrompt: "Updated prompt");

        var result = await sut.UpdateAsync(modelId, dto, CancellationToken.None);

        Assert.Equal("New Name", result.CommercialName);
        Assert.Equal("new-tech", result.TechnicalName);
        Assert.Equal(16000, result.TokenLimit);
        Assert.Equal(0.3, result.DefaultTemperature);
        Assert.Equal(Domain.Agents.BotType.Agent, result.DefaultBotType);
        modelRepo.Verify(r => r.UpdateAsync(model), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenModelMissing_Throws()
    {
        var providerId = Guid.NewGuid();
        var provider = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "key");
        var modelRepo = new Mock<IAIModelRepository>();
        var providerRepo = new Mock<IAIProviderRepository>();
        providerRepo.Setup(r => r.GetByIdAsync(providerId, It.IsAny<CancellationToken>())).ReturnsAsync(provider);
        modelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIModel?)null);

        var sut = CreateService(modelRepo, providerRepo);
        var dto = new UpdateAIModelDto(providerId, "Name", "tech", 8192, "chat");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndCommits()
    {
        var modelId = Guid.NewGuid();
        var model = new AIModel(modelId, Guid.NewGuid(), "GPT", "gpt", 8192, "chat");
        var modelRepo = new Mock<IAIModelRepository>();
        modelRepo.Setup(r => r.GetByIdAsync(modelId, It.IsAny<CancellationToken>())).ReturnsAsync(model);
        modelRepo.Setup(r => r.DeleteAsync(model)).Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        var sut = new AIModelService(
            modelRepo.Object,
            new Mock<IAIProviderRepository>().Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIModelService>>());

        await sut.DeleteAsync(modelId, CancellationToken.None);

        modelRepo.Verify(r => r.DeleteAsync(model), Times.Once);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_DoesNothing()
    {
        var modelRepo = new Mock<IAIModelRepository>();
        modelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIModel?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        var sut = new AIModelService(
            modelRepo.Object,
            new Mock<IAIProviderRepository>().Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIModelService>>());

        await sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        modelRepo.Verify(r => r.DeleteAsync(It.IsAny<AIModel>()), Times.Never);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AIModelService CreateService(
        Mock<IAIModelRepository> modelRepo,
        Mock<IAIProviderRepository> providerRepo)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        return new AIModelService(
            modelRepo.Object,
            providerRepo.Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIModelService>>());
    }
}
