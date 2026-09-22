using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace MyApp.Agentic.Application.AI;

/// <summary>Factory methods for creating provider-specific <see cref="IChatClient"/> adapters.</summary>
public static class AgentAdapterFactory
{
    /// <summary>
    /// Creates a hugging face client.
    /// compatible with Hugging Face router models.
    /// </summary>
    /// <param name="modelId">The model Id.</param>
    /// <param name="hfToken">The hf Token.</param>
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

    /// <summary>Ensures the model identifier ends with the <c>:auto</c> routing suffix required by the HuggingFace router.</summary>
    /// Ensure auto suffix.
    /// <param name="modelId">The model Id.</param>
    /// <returns>Model identifier guaranteed to end with <c>:auto</c>.</returns>
    public static string EnsureAutoSuffix(string modelId) =>
        modelId.EndsWith(":auto", StringComparison.OrdinalIgnoreCase)
            ? modelId
            : $"{modelId}:auto";
}
