using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Security;

namespace MyApp.Agentic.Application.Tests;

public class AIProviderServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsConfiguredDefaults()
    {
        AIProvider? captured = null;
        var repo = new Mock<IAIProviderRepository>();
        var crypto = new Mock<ISecretCryptoService>();
        crypto.Setup(c => c.Encrypt(It.IsAny<string>())).Returns((string s) => $"enc::{s}");
        repo.Setup(r => r.AddAsync(It.IsAny<AIProvider>()))
            .Callback<AIProvider>(p => captured = p)
            .ReturnsAsync((AIProvider p) => p);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIProviderService(repo.Object, crypto.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIProviderService>>());
        var dto = new CreateAIProviderDto(
            "OpenAI",
            "https://api.openai.com/v1",
            "sk-test-key",
            DefaultTemperature: 1.2,
            DefaultTopK: 5,
            DefaultMaxTokens: 5000,
            DefaultEmbeddingDimensions: 3072,
            DefaultEnableMemory: false,
            DefaultEnableRAG: true,
            DefaultEmbeddingModelName: "text-embedding-3-large",
            DefaultBotType: BotType.Agent,
            DefaultSystemPrompt: "Provider prompt");

        var result = await sut.CreateAsync(dto);

        Assert.NotNull(captured);
        Assert.Equal(1.2, captured!.DefaultTemperature);
        Assert.Equal(5, captured.DefaultTopK);
        Assert.Equal(5000, captured.DefaultMaxTokens);
        Assert.Equal(3072, captured.DefaultEmbeddingDimensions);
        Assert.False(captured.DefaultEnableMemory);
        Assert.True(captured.DefaultEnableRAG);
        Assert.Equal("text-embedding-3-large", captured.DefaultEmbeddingModelName);
        Assert.Equal(BotType.Agent, captured.DefaultBotType);
        Assert.Equal("Provider prompt", captured.DefaultSystemPrompt);
        Assert.Equal("enc::sk-test-key", captured.EncryptedApiKey);

        Assert.Equal("OpenAI", result.Name);
        Assert.True(result.HasApiKey);
        Assert.Equal("enc::sk-test-key", result.ApiKey);
        Assert.Equal(1.2, result.DefaultTemperature);
        Assert.Equal(BotType.Agent, result.DefaultBotType);
        crypto.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesConfiguredDefaults()
    {
        var providerId = Guid.NewGuid();
        var existing = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "sk-old-key");
        var repo = new Mock<IAIProviderRepository>();
        var crypto = new Mock<ISecretCryptoService>();
        crypto.Setup(c => c.Encrypt(It.IsAny<string>())).Returns((string s) => $"enc::{s}");
        repo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIProviderService(repo.Object, crypto.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIProviderService>>());
        var dto = new UpdateAIProviderDto(
            "OpenAI",
            "https://api.openai.com/v1",
            "sk-new-key",
            DefaultTemperature: 0.9,
            DefaultTopK: 8,
            DefaultMaxTokens: 6000,
            DefaultEmbeddingDimensions: 4096,
            DefaultEnableMemory: true,
            DefaultEnableRAG: false,
            DefaultEmbeddingModelName: "embed-v2",
            DefaultBotType: BotType.Chat,
            DefaultSystemPrompt: "Updated provider prompt");

        var result = await sut.UpdateAsync(providerId, dto);

        Assert.Equal(0.9, existing.DefaultTemperature);
        Assert.Equal(8, existing.DefaultTopK);
        Assert.Equal(6000, existing.DefaultMaxTokens);
        Assert.Equal(4096, existing.DefaultEmbeddingDimensions);
        Assert.True(existing.DefaultEnableMemory);
        Assert.False(existing.DefaultEnableRAG);
        Assert.Equal("embed-v2", existing.DefaultEmbeddingModelName);
        Assert.Equal(BotType.Chat, existing.DefaultBotType);
        Assert.Equal("Updated provider prompt", existing.DefaultSystemPrompt);
        Assert.Equal("enc::sk-new-key", existing.EncryptedApiKey);
        Assert.True(result.HasApiKey);
        Assert.Equal("enc::sk-new-key", result.ApiKey);
        Assert.Equal(existing.DefaultTemperature, result.DefaultTemperature);
        crypto.Verify(c => c.Encrypt("sk-new-key"), Times.Once);
        crypto.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenApiKeyIsSameEncryptedValue_DoesNotReEncrypt()
    {
        var providerId = Guid.NewGuid();
        var existing = new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "enc::sk-same-key");
        var repo = new Mock<IAIProviderRepository>();
        var crypto = new Mock<ISecretCryptoService>();
        repo.Setup(r => r.GetByIdAsync(providerId)).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIProviderService(repo.Object, crypto.Object, unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<AIProviderService>>());
        var dto = new UpdateAIProviderDto(
            "OpenAI",
            "https://api.openai.com/v1",
            "enc::sk-same-key");

        var result = await sut.UpdateAsync(providerId, dto);

        Assert.Equal("enc::sk-same-key", existing.EncryptedApiKey);
        Assert.True(result.HasApiKey);
        Assert.Equal("enc::sk-same-key", result.ApiKey);
        crypto.Verify(c => c.Encrypt(It.IsAny<string>()), Times.Never);
        crypto.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_ReturnsProvidersOrderedByName()
    {
        var providers = new List<AIProvider>
        {
            new(Guid.NewGuid(), "Zebra", "https://z.example.com", "key"),
            new(Guid.NewGuid(), "Alpha", "https://a.example.com", "key")
        };
        var repo = new Mock<IAIProviderRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(providers);

        var sut = CreateService(repo);
        var result = (await sut.ListAsync(CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var provider = new AIProvider(id, "OpenAI", "https://api.openai.com/v1", "enc::key");
        var repo = new Mock<IAIProviderRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(provider);

        var sut = CreateService(repo);
        var result = await sut.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("OpenAI", result.Name);
        Assert.True(result.HasApiKey);
        Assert.Equal("enc::key", result.ApiKey);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var repo = new Mock<IAIProviderRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProvider?)null);

        var sut = CreateService(repo);
        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithBlankApiKey_DoesNotEncrypt()
    {
        AIProvider? captured = null;
        var repo = new Mock<IAIProviderRepository>();
        var crypto = new Mock<ISecretCryptoService>();
        repo.Setup(r => r.AddAsync(It.IsAny<AIProvider>()))
            .Callback<AIProvider>(p => captured = p)
            .ReturnsAsync((AIProvider p) => p);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIProviderService(
            repo.Object,
            crypto.Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIProviderService>>());

        var result = await sut.CreateAsync(
            new CreateAIProviderDto("Local", "http://localhost", "   "),
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Null(captured!.EncryptedApiKey);
        Assert.False(result.HasApiKey);
        crypto.Verify(c => c.Encrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenProviderMissing_Throws()
    {
        var repo = new Mock<IAIProviderRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProvider?)null);

        var sut = CreateService(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), new UpdateAIProviderDto("Name", "https://x", null), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndCommits()
    {
        var id = Guid.NewGuid();
        var provider = new AIProvider(id, "OpenAI", "https://api.openai.com/v1", "key");
        var repo = new Mock<IAIProviderRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(provider);
        repo.Setup(r => r.DeleteAsync(provider)).Returns(Task.CompletedTask);

        var crypto = new Mock<ISecretCryptoService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIProviderService(
            repo.Object,
            crypto.Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIProviderService>>());

        await sut.DeleteAsync(id, CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(provider), Times.Once);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_DoesNothing()
    {
        var repo = new Mock<IAIProviderRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProvider?)null);

        var crypto = new Mock<ISecretCryptoService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var sut = new AIProviderService(
            repo.Object,
            crypto.Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIProviderService>>());

        await sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(It.IsAny<AIProvider>()), Times.Never);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AIProviderService CreateService(Mock<IAIProviderRepository> repo)
    {
        var crypto = new Mock<ISecretCryptoService>();
        crypto.Setup(c => c.Encrypt(It.IsAny<string>())).Returns((string s) => $"enc::{s}");
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        return new AIProviderService(
            repo.Object,
            crypto.Object,
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<AIProviderService>>());
    }
}
