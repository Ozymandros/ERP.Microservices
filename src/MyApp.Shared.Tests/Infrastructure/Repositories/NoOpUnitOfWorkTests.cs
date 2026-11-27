using FluentAssertions;
using MyApp.Shared.Infrastructure.Repositories;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Repositories;

public class NoOpUnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_ReturnsEmptyCollection()
    {
        var sut = new NoOpUnitOfWork();

        var result = await sut.CommitAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_WithDefaultToken_ReturnsEmptyCollection()
    {
        var sut = new NoOpUnitOfWork();

        var result = await sut.CommitAsync();

        result.Should().BeEmpty();
    }
}
