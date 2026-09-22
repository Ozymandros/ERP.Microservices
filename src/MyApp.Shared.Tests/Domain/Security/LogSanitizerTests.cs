using FluentAssertions;
using MyApp.Shared.Domain.Security;

namespace MyApp.Shared.Tests.Domain.Security;

public class LogSanitizerTests
{
    private readonly ILogSanitizer _sut = new LogSanitizer();

    [Fact]
    public void Sanitize_Null_ReturnsEmpty()
    {
        _sut.Sanitize(null).Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_Empty_ReturnsEmpty()
    {
        _sut.Sanitize(string.Empty).Should().BeEmpty();
    }

    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("Admin", "Admin")]
    public void Sanitize_Passthrough_PreservesSafeValues(string input, string expected)
    {
        _sut.Sanitize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("evil\nline", "evilline")]
    [InlineData("evil\rline", "evilline")]
    [InlineData("evil\r\nline", "evilline")]
    [InlineData("a\nb\rc", "abc")]
    public void Sanitize_StripsNewlines(string input, string expected)
    {
        _sut.Sanitize(input).Should().Be(expected);
    }
}
