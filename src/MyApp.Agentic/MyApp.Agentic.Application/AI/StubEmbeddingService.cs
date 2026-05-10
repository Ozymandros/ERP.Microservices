using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Application.AI;

public class StubEmbeddingService : IEmbeddingService
{
    private readonly ILogger<StubEmbeddingService> _logger;
    private const int EmbeddingDimensions = 1536;

    public StubEmbeddingService(ILogger<StubEmbeddingService> logger)
    {
        _logger = logger;
    }

    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating stub embedding for text of length {Length}", text.Length);

        var random = new Random(text.GetHashCode());
        var embedding = new float[EmbeddingDimensions];
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            embedding[i] = (float)(embedding[i] / magnitude);
        }

        return Task.FromResult(new ReadOnlyMemory<float>(embedding));
    }
}
