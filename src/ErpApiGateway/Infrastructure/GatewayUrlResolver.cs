using Microsoft.AspNetCore.Http;

namespace ErpApiGateway.Infrastructure;

/// <summary>
/// Resolves the public base URL for Scalar and similar clients.
/// Uses <c>Gateway:PublicBaseUrl</c> when set; otherwise the current request (after forwarded headers).
/// </summary>
public static class GatewayUrlResolver
{
    private const string PublicBaseUrlKey = "Gateway:PublicBaseUrl";

    public static string GetPublicBaseUrl(HttpContext httpContext, IConfiguration configuration)
    {
        var configured = configuration[PublicBaseUrlKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var request = httpContext.Request;
        var fromRequest = $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');

        // When browsing Scalar via localhost inside a dev container, prefer the configured Ocelot public URL.
        if (IsLoopbackHost(request.Host.Host))
        {
            var ocelotBase = configuration["GlobalConfiguration:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(ocelotBase) && !IsLoopbackHost(GetHostFromUrl(ocelotBase)))
            {
                return ocelotBase.TrimEnd('/');
            }
        }

        return fromRequest;
    }

    private static bool IsLoopbackHost(string? host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);

    private static string? GetHostFromUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
}
