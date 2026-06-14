using Microsoft.Extensions.Primitives;

namespace MyApp.Shared.Domain.Authentication;

/// <summary>
/// Extracts the raw JWT from an Authorization header value.
/// </summary>
public static class BearerTokenHelper
{
    public static string? ExtractToken(StringValues authorizationHeader)
    {
        if (authorizationHeader.Count == 0)
            return null;

        var value = authorizationHeader.ToString().Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        const string prefix = "Bearer ";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return value[prefix.Length..].Trim();

        return value;
    }
}
