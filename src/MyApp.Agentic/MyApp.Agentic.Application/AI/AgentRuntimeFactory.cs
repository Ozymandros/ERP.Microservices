using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace MyApp.Agentic.Application.AI;

/// <summary>Creates provider-specific <see cref="IChatClient"/> instances based on the agent execution context.</summary>
public class AgentRuntimeFactory : IAgentRuntimeFactory
{
    /// <summary>Creates a <see cref="IChatClient"/> configured for the provider associated with the agent.</summary>
    /// Creates a client.
    /// <param name="context">The context.</param>
    /// <returns>Configured chat client for the resolved provider.</returns>
    public IChatClient CreateClient(AgentExecutionContext context)
    {
        var provider = context.Agent.Model?.Provider?.Name ?? string.Empty;
        var modelId = context.Agent.Model?.TechnicalName ?? string.Empty;

        return UsesOpenAICompatibleRuntime(provider)
            ? CreateOpenAIClient(modelId, context.ApiKey, context.BaseUrl)
            : CreateHuggingFaceClient(modelId, context.ApiKey, context.BaseUrl);
    }

    private static bool UsesOpenAICompatibleRuntime(string provider) =>
        !string.Equals(provider, "HuggingFace", StringComparison.OrdinalIgnoreCase);

    private static IChatClient CreateOpenAIClient(string modelId, string apiKey, string baseUrl)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            options.Endpoint = new Uri(baseUrl);
        }

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(modelId).AsIChatClient();
    }

    private static IChatClient CreateHuggingFaceClient(string modelId, string apiKey, string baseUrl)
    {
        var finalModelId = AgentAdapterFactory.EnsureAutoSuffix(modelId);
        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) 
            ? "https://router.huggingface.co/v1" 
            : baseUrl;

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(effectiveBaseUrl)
        };

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(finalModelId).AsIChatClient();
    }
}
