namespace MyApp.Agentic.Infrastructure.Memory;

public interface IMemoryEmbeddingGenerator
{
    int VectorSize { get; }
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        MemoryEmbeddingProviderContext provider,
        CancellationToken cancellationToken = default);
}
