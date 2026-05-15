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
        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }
}
