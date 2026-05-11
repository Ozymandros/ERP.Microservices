namespace MyApp.Agentic.Application.AI;

public interface IEmbeddingService
{
    Task<string> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
