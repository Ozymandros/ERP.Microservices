using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Application.AI;

public class StubEmbeddingService : IEmbeddingService
{
    private readonly ILogger<StubEmbeddingService> _logger;

    public StubEmbeddingService(ILogger<StubEmbeddingService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating stub embedding for text of length {Length}", text.Length);

        return Task.FromResult(text.Trim());
    }
}
