namespace MyApp.Agentic.Application.AI;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
