using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Application.AI;

/// <summary>No-op <see cref="IEmbeddingService"/> implementation that returns the trimmed input text as its own embedding, for use in development and testing.</summary>
public class StubEmbeddingService : IEmbeddingService
{
    private readonly ILogger<StubEmbeddingService> _logger;

    /// <summary>Initializes a new instance of the <see cref="StubEmbeddingService"/> class.</summary>
    /// Initializes a new instance of the StubEmbeddingService class.
    /// <param name="logger">The logger.</param>
    public StubEmbeddingService(ILogger<StubEmbeddingService> logger)
    {
        _logger = logger;
    }

    /// <summary>Returns the trimmed input text as a stub embedding without calling an external service.</summary>
    /// Generate embedding asynchronously.
    /// <param name="text">The text.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>Trimmed input text.</returns>
    public Task<string> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating stub embedding for text of length {Length}", text.Length);

        return Task.FromResult(text.Trim());
    }
}
