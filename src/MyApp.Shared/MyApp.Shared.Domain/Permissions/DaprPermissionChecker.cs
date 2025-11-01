using Dapr.Client;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

public class DaprPermissionChecker : IPermissionChecker
{
    private readonly DaprClient _daprClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DaprPermissionChecker(DaprClient daprClient, IHttpContextAccessor httpContextAccessor)
    {
        _daprClient = daprClient;
        _httpContextAccessor = httpContextAccessor;
    }

    // Opció 2 (millor): El token es passa com a paràmetre (més flexible)
    public async Task<bool> HasPermissionAsync(Guid userId, string module, string action)
    {
        var query = new Dictionary<string, string>
        {
            ["userId"] = userId.ToString(),
            ["module"] = module,
            ["action"] = action
        };

        // 1. Crea la petició manualment
        var request = _daprClient.CreateInvokeMethodRequest(HttpMethod.Get, "auth-service", "api/permissions/check", query);

        // 2. Afegeix el header d'autenticació
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is true)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader.ToString().Replace("Bearer ", ""));
        }

        // 3. Fes la crida
        try
        {
            var result = await _daprClient.InvokeMethodAsync<bool>(request);
            return result;
        }
        catch (InvocationException)
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

        // 1. Crea la petició
        using var request = _daprClient.CreateInvokeMethodRequest(HttpMethod.Get, "auth-service", "api/permissions/check", query);

        // 2. Afegeix el header d'autenticació
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is true)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader.ToString().Replace("Bearer ", ""));
        }

        // 3. Fes la crida
        try
        {
            var result = await _daprClient.InvokeMethodAsync<bool>(request);
            return result;
        }
        catch (InvocationException)
        {
            return false;
        }
    }
}