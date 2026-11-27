using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;

namespace MyApp.Agentic.Application.Tests;

public class StubAgentExecutionServiceTests
{
    private readonly StubAgentExecutionService _sut = new(Mock.Of<ILogger<StubAgentExecutionService>>());

    private static AgentExecutionContext CreateContext(
        string? baseUrl = "https://api.example.com/v1",
        IEnumerable<string>? memories = null,
        IEnumerable<ToolDefinition>? tools = null,
        AIProvider? provider = null)
    {
        var agent = new Agent(
            Guid.NewGuid(),
            "Stub Agent",
            "Test agent",
            Guid.NewGuid(),
            0.5,
            "You are helpful");

        if (provider is not null)
        {
            var model = new AIModel(
                Guid.NewGuid(),
                provider.Id,
                "HF Model",
                "org/model",
                8192,
                "chat");
            model.SetProviderForTest(provider);
            agent.SetModelForTest(model);
        }

        return new AgentExecutionContext
        {
            Agent = agent,
            ApiKey = "hf-test-token",
            BaseUrl = baseUrl!,
            SystemPrompt = "sys",
            Temperature = 0.5,
            MaxTokens = 1024,
            ContextMemories = memories?.ToList() ?? new List<string>(),
            Tools = tools?.ToList() ?? new List<ToolDefinition>()
        };
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsStubResponseWithUserMessage()
    {
        var context = CreateContext();

        var result = await _sut.ExecuteAsync(context, "Hello agent", CancellationToken.None);

        Assert.Contains("Hello agent", result.Content);
        Assert.Contains("stub response", result.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(context.BaseUrl, result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithMemories_AppendsRelevantContext()
    {
        var context = CreateContext(memories: new[] { "memory-one", "memory-two", "memory-three" });

        var result = await _sut.ExecuteAsync(context, "query", CancellationToken.None);

        Assert.Contains("Relevant context", result.Content);
        Assert.Contains("memory-one", result.Content);
        Assert.Contains("memory-two", result.Content);
        Assert.DoesNotContain("memory-three", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithTools_AppendsAvailableTools()
    {
        var context = CreateContext(tools: new[]
        {
            new ToolDefinition("get_product_by_name", "/api/products"),
            new ToolDefinition("create_order", "/api/orders")
        });

        var result = await _sut.ExecuteAsync(context, "query", CancellationToken.None);

        Assert.Contains("Available tools", result.Content);
        Assert.Contains("get_product_by_name", result.Content);
        Assert.Contains("create_order", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHuggingFaceProvider_StillReturnsStubResponse()
    {
        var provider = new AIProvider(Guid.NewGuid(), "HuggingFace", "https://router.huggingface.co/v1", "enc-key");
        var context = CreateContext(provider: provider);

        var result = await _sut.ExecuteAsync(context, "hf prompt", CancellationToken.None);

        Assert.Contains("hf prompt", result.Content);
        Assert.Contains("stub response", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCanceled_ThrowsOperationCanceledException()
    {
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.ExecuteAsync(context, "canceled", cts.Token));
    }
}
