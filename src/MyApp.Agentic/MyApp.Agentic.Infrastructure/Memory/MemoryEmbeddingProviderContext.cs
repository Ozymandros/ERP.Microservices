namespace MyApp.Agentic.Infrastructure.Memory;

/// <summary>
/// Provider settings used when generating memory embeddings for a specific agent request.
/// </summary>
/// <param name="ApiKey">Decrypted API key for the agent's selected model provider.</param>
/// <param name="BaseUrl">Provider base URL used to build the embeddings endpoint.</param>
/// <param name="EmbeddingModelName">Embedding model identifier for the provider.</param>
public sealed record MemoryEmbeddingProviderContext(
    string ApiKey,
    string BaseUrl,
    string EmbeddingModelName);
