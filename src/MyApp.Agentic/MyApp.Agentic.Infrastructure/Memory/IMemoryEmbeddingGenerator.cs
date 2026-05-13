namespace MyApp.Agentic.Infrastructure.Memory;

/// <summary>
/// Generates vector embeddings for agent memory persistence and similarity search.
/// </summary>
public interface IMemoryEmbeddingGenerator
{
    /// <summary>
    /// Gets the vector size produced by this generator.
    /// </summary>
    int VectorSize { get; }

    /// <summary>
    /// Generates an embedding vector for the supplied text using the agent's provider context.
    /// </summary>
    /// <param name="text">Source text to embed.</param>
    /// <param name="provider">Provider credentials and embedding model configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Floating-point embedding vector.</returns>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        MemoryEmbeddingProviderContext provider,
        CancellationToken cancellationToken = default);
}
