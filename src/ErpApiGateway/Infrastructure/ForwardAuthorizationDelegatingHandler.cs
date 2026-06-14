namespace ErpApiGateway.Infrastructure;

/// <summary>
/// Ensures the inbound Authorization header reaches downstream services after Ocelot gateway JWT authentication.
/// </summary>
public sealed class ForwardAuthorizationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var upstreamAuthorization = httpContext?.Items["OcelotForwardAuthorization"] as string;
        if (string.IsNullOrWhiteSpace(upstreamAuthorization)
            && httpContext?.Request.Headers.TryGetValue("Authorization", out var authValues) == true)
        {
            upstreamAuthorization = authValues.ToString();
        }

        var downstreamAuthorization = request.Headers.Authorization?.ToString();
        var needsReplace = string.IsNullOrWhiteSpace(downstreamAuthorization)
            || downstreamAuthorization.Contains("Header:Authorization", StringComparison.OrdinalIgnoreCase)
            || !downstreamAuthorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(upstreamAuthorization) && needsReplace)
        {
            request.Headers.Remove("Authorization");
            request.Headers.TryAddWithoutValidation("Authorization", upstreamAuthorization);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
