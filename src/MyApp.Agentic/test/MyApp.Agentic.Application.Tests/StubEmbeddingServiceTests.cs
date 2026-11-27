using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.AI;

namespace MyApp.Agentic.Application.Tests;

public class StubEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_ReturnsTrimmedText()
    {
        var sut = new StubEmbeddingService(Mock.Of<ILogger<StubEmbeddingService>>());

        var result = await sut.GenerateEmbeddingAsync("  hello world  ", CancellationToken.None);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyAfterTrim_ReturnsEmpty()
    {
        var sut = new StubEmbeddingService(Mock.Of<ILogger<StubEmbeddingService>>());

        var result = await sut.GenerateEmbeddingAsync("   ", CancellationToken.None);

        Assert.Equal(string.Empty, result);
    }
}
