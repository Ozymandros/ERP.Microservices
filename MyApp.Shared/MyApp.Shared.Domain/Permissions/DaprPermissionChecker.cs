using Dapr.Client;

public class DaprPermissionChecker : IPermissionChecker, IDisposable
{
    private readonly DaprClient _daprClient;

    public DaprPermissionChecker(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public void Dispose()
    {
        _daprClient.Dispose();
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string module, string action)
    {
        var query = new Dictionary<string, string>
        {
            ["userId"] = userId.ToString(),
            ["module"] = module,
            ["action"] = action
        };

        var result = await _daprClient.InvokeMethodAsync<Dictionary<string, string>, bool>(
            "auth-service", // Nom del servei registrat a Dapr
            "permissions/check",
            query
        );

        return result;
    }

    public async Task<bool> HasPermissionAsync(string? username, string module, string action)
    {
        if (string.IsNullOrEmpty(username))
        {
            throw new ArgumentException($"'{nameof(username)}' cannot be null or empty.", nameof(username));
        }

        if (string.IsNullOrEmpty(module))
        {
            throw new ArgumentException($"'{nameof(module)}' cannot be null or empty.", nameof(module));
        }

        if (string.IsNullOrEmpty(action))
        {
            throw new ArgumentException($"'{nameof(action)}' cannot be null or empty.", nameof(action));
        }

        var query = new Dictionary<string, string>
        {
            ["username"] = username,
            ["module"] = module,
            ["action"] = action
        };

        var result = await _daprClient.InvokeMethodAsync<Dictionary<string, string>, bool>(
            "auth-service", // Nom del servei registrat a Dapr
            "permissions/check",
            query
        );

        return result;
    }
}