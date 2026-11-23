using FluentAssertions;
using Microsoft.Extensions.Primitives;
using MyApp.Shared.Domain.Authentication;

namespace MyApp.Shared.Tests.Domain.Authentication;

public class BearerTokenHelperTests
{
    [Fact]
    public void ExtractToken_EmptyHeader_ReturnsNull()
    {
        BearerTokenHelper.ExtractToken(default).Should().BeNull();
        BearerTokenHelper.ExtractToken(StringValues.Empty).Should().BeNull();
    }

    [Theory]
    [InlineData("Bearer abc.def.ghi", "abc.def.ghi")]
    [InlineData("bearer xyz", "xyz")]
    [InlineData("raw-token", "raw-token")]
    public void ExtractToken_ParsesBearerOrRawValue(string header, string expected) =>
        BearerTokenHelper.ExtractToken(new StringValues(header)).Should().Be(expected);

    [Fact]
    public void ExtractToken_WhitespaceOnly_ReturnsNull() =>
        BearerTokenHelper.ExtractToken(new StringValues("   ")).Should().BeNull();
}