namespace MyApp.Agentic.Infrastructure.Memory;

/// <summary>
/// Memory embedding provider context.
/// </summary>
/// <param name="ApiKey">The api Key.</param>
/// <param name="BaseUrl">The base Url.</param>
/// <param name="EmbeddingModelName">The embedding Model Name.</param>
public sealed record MemoryEmbeddingProviderContext(
    string ApiKey,
    string BaseUrl,
    string EmbeddingModelName);
