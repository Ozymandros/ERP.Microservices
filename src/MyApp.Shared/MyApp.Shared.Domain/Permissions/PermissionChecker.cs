using Microsoft.AspNetCore.Http;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Authentication;
using System.Net.Http.Headers;

namespace MyApp.Shared.Domain.Permissions;

/// <summary>
/// Service for checking user permissions across microservices
/// </summary>
public class PermissionChecker : IPermissionChecker
{
    private readonly IServiceInvoker _serviceInvoker;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionChecker(IServiceInvoker serviceInvoker, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(serviceInvoker);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _serviceInvoker = serviceInvoker;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string module, string action)
    {
        var query = new Dictionary<string, string?>
        {
            ["userId"] = userId.ToString(),
            ["module"] = module,
            ["action"] = action
        };

        // 1. Create the request manually
        var request = _serviceInvoker.CreateRequest(
            ServiceNames.Auth,
            "api/internal/permissions/check",
            HttpMethod.Get,
            null,
            query);

        AttachBearerToken(request);

        // 3. Make the call via Dapr
        try
        {
            var result = await _serviceInvoker.InvokeAsync<bool>(request);
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> HasPermissionAsync(string module, string action)
    {
        if (string.IsNullOrEmpty(module))
            throw new ArgumentException($"'{nameof(module)}' cannot be null or empty.", nameof(module));
        if (string.IsNullOrEmpty(action))
            throw new ArgumentException($"'{nameof(action)}' cannot be null or empty.", nameof(action));

        var userId = GetUserIdFromHttpContext();
        if (userId.HasValue)
            return await HasPermissionAsync(userId.Value, module, action);

        var query = new Dictionary<string, string?>
        {
            ["module"] = module,
            ["action"] = action
        };

        // 1. Create the request
        using var request = _serviceInvoker.CreateRequest(
            ServiceNames.Auth,
            "api/internal/permissions/check",
            HttpMethod.Get,
            null,
            query);

        AttachBearerToken(request);

        // 3. Make the call via Dapr
        try
        {
            var result = await _serviceInvoker.InvokeAsync<bool>(request);
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void AttachBearerToken(HttpRequestMessage request)
    {
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is not true)
            return;

        var token = BearerTokenHelper.ExtractToken(authHeader);
        if (string.IsNullOrWhiteSpace(token))
            return;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private Guid? GetUserIdFromHttpContext()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated is not true)
            return null;

        var id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(id, out var userId) ? userId : null;
    }
}
