namespace MyApp.Agentic.Application.AI;

/// <summary>Service contract for generating text embeddings used in semantic memory retrieval.</summary>
public interface IEmbeddingService
{
    /// <summary>Generates an embedding representation for the provided text.</summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Embedding representation of the text.</returns>
    Task<string> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
