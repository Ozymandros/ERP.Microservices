namespace MyApp.Agentic.Infrastructure.Memory;

public sealed record MemoryEmbeddingProviderContext(
    string ApiKey,
    string BaseUrl,
    string EmbeddingModelName);
