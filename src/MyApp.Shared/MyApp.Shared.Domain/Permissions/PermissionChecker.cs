using Microsoft.AspNetCore.Http;
using MyApp.Shared.Domain.Messaging;
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
        _serviceInvoker = serviceInvoker ?? throw new ArgumentNullException(nameof(serviceInvoker));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string module, string action)
    {
        var query = new Dictionary<string, string>
        {
            ["userId"] = userId.ToString(),
            ["module"] = module,
            ["action"] = action
        };

        // 1. Create the request manually
        var request = _serviceInvoker.CreateRequest(
            "auth-service",
            "api/permissions/check",
            HttpMethod.Get,
            null,
            query);

        // 2. Add the authentication header
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is true)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader.ToString().Replace("Bearer ", ""));
        }

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

        var query = new Dictionary<string, string>
        {
            ["module"] = module,
            ["action"] = action
        };

        // 1. Create the request
        using var request = _serviceInvoker.CreateRequest(
            "auth-service",
            "api/permissions/check",
            HttpMethod.Get,
            null,
            query);

        // 2. Add the authentication header
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is true)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader.ToString().Replace("Bearer ", ""));
        }

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
}
