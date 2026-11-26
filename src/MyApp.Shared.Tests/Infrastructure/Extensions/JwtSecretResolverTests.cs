using FluentAssertions;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Shared.Tests.Infrastructure.Extensions;

public class JwtSecretResolverTests
{
    [Fact]
    public void GetRequiredSecretKey_WhenUnset_Throws()
    {
        var original = Environment.GetEnvironmentVariable(JwtSecretResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(JwtSecretResolver.EnvironmentVariableName, null);
            var act = () => JwtSecretResolver.GetRequiredSecretKey();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{JwtSecretResolver.EnvironmentVariableName}*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(JwtSecretResolver.EnvironmentVariableName, original);
        }
    }

    [Fact]
    public void GetRequiredSecretKey_WhenSet_ReturnsValue()
    {
        var original = Environment.GetEnvironmentVariable(JwtSecretResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(JwtSecretResolver.EnvironmentVariableName, "test-secret-key");
            JwtSecretResolver.GetRequiredSecretKey().Should().Be("test-secret-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable(JwtSecretResolver.EnvironmentVariableName, original);
        }
    }
}