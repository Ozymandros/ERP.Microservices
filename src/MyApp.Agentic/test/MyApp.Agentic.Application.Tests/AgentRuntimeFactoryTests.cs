using System.Reflection;
using Microsoft.Extensions.AI;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Tests;

public class AgentRuntimeFactoryTests
{
    [Theory]
    [InlineData("OpenAI", true)]
    [InlineData("OpenCode", true)]
    [InlineData("Deepseek", true)]
    [InlineData("HuggingFace", false)]
    public void UsesOpenAICompatibleRuntime_MapsProvidersToExpectedRuntime(string providerName, bool expectedRuntimeKind)
    {
        var result = InvokeUsesOpenAICompatibleRuntime(providerName);

        Assert.Equal(expectedRuntimeKind, result);
    }

    [Fact]
    public void CreateClient_UnknownProvider_UsesOpenAICompatibleFallback()
    {
        var factory = new AgentRuntimeFactory();
        var context = CreateContext("Deepseek", "https://api.deepseek.com/v1", "deepseek-chat");

        var client = factory.CreateClient(context);

        Assert.NotNull(client);
        Assert.IsAssignableFrom<IChatClient>(client);
    }

    private static bool InvokeUsesOpenAICompatibleRuntime(string providerName)
    {
        var method = typeof(AgentRuntimeFactory).GetMethod(
            "UsesOpenAICompatibleRuntime",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return Assert.IsType<bool>(method!.Invoke(null, [providerName]));
    }

    private static AgentExecutionContext CreateContext(string providerName, string baseUrl, string modelId)
    {
        var provider = new AIProvider(Guid.NewGuid(), providerName, baseUrl, "sk-test-key");
        var model = new AIModel(Guid.NewGuid(), provider.Id, modelId, modelId, 8192, "chat,tool-calling");
        SetProvider(model, provider);

        var agent = new Agent(
            Guid.NewGuid(),
            "Test Agent",
            "Routes provider runtime requests",
            model.Id,
            0.7,
            "You are a test assistant.");

        agent.SetModel(model);

        return new AgentExecutionContext
        {
            Agent = agent,
            ApiKey = "sk-test-key",
            BaseUrl = baseUrl,
            MaxTokens = 256,
            Temperature = 0.7
        };
    }

    private static void SetProvider(AIModel model, AIProvider provider)
    {
        var backingField = typeof(AIModel).GetField("<Provider>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(backingField);
        backingField!.SetValue(model, provider);
    }
}
