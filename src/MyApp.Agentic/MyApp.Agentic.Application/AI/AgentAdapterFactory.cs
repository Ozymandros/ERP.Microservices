using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace MyApp.Agentic.Application.AI;

public static class AgentAdapterFactory
{
    /// <summary>
    /// Creates a generic client for the Microsoft Agents SDK
    /// compatible with Hugging Face router models.
    /// </summary>
    public static IChatClient CreateHuggingFaceClient(string modelId, string hfToken)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID is required.", nameof(modelId));

        if (string.IsNullOrWhiteSpace(hfToken))
            throw new ArgumentException("Hugging Face token is required.", nameof(hfToken));

        var finalModelId = EnsureAutoSuffix(modelId);

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://router.huggingface.co/v1")
        };

        var openAiClient = new OpenAIClient(new ApiKeyCredential(hfToken), options);
        ChatClient chatClient = openAiClient.GetChatClient(finalModelId);
        return chatClient.AsIChatClient();
    }

    public static string EnsureAutoSuffix(string modelId) =>
        modelId.EndsWith(":auto", StringComparison.OrdinalIgnoreCase)
            ? modelId
            : $"{modelId}:auto";
}
