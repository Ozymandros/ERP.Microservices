using Dapr.Client;

public class DaprPermissionChecker : IPermissionChecker
{
    private readonly DaprClient _daprClient;

    public DaprPermissionChecker(DaprClient daprClient)
    {
        _daprClient = daprClient;
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
            HttpMethod.Get, // 👈 FORÇA GET
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

        // 1. Construeix el query string
        var queryString = new FormUrlEncodedContent(query).ReadAsStringAsync().Result;

        // 2. Construeix la URL completa (mètode + query string)
        var fullMethod = $"permissions/check?{queryString}";

        // 3. Invoca Dapr SENSE cos de petició
        var result = await _daprClient.InvokeMethodAsync<bool>(
            HttpMethod.Get,
            "auth-service", // Nom del servei registrat a Dapr
            fullMethod // URL COMPLETA amb el query string
        );
        //var result = await _daprClient.InvokeMethodAsync<Dictionary<string, string>, bool>(
        //    HttpMethod.Get,
        //    "auth-service", // Nom del servei registrat a Dapr
        //    "permissions/check",
        //    query
        //);

        return result;
    }
}