using Moq;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;
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

        var sut = new AIProviderService(repo.Object, crypto.Object);
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
        Assert.Equal(1.2, result.DefaultTemperature);
        Assert.Equal(BotType.Agent, result.DefaultBotType);
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

        var sut = new AIProviderService(repo.Object, crypto.Object);
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
        Assert.Equal(existing.DefaultTemperature, result.DefaultTemperature);
    }
}
