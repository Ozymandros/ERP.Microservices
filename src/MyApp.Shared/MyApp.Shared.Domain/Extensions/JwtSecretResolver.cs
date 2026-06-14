namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Resolves the JWT signing secret from environment variables only (never from tracked configuration files).
/// </summary>
public static class JwtSecretResolver
{
    public const string EnvironmentVariableName = "Jwt__SecretKey";

    public static string GetRequiredSecretKey()
    {
        var secretKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                $"JWT secret is not configured. Set the {EnvironmentVariableName} environment variable.");
        }

        return secretKey;
    }
}
