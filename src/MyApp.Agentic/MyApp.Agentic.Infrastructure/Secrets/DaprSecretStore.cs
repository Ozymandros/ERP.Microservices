using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Infrastructure.Secrets;

public interface ISecretStore
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default);
}

public class DaprSecretStore : ISecretStore
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprSecretStore> _logger;
    private const string SecretStoreName = "secretstore";

    public DaprSecretStore(DaprClient daprClient, ILogger<DaprSecretStore> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var secrets = await _daprClient.GetSecretAsync(SecretStoreName, secretName, cancellationToken: cancellationToken);
            return secrets.TryGetValue(secretName, out var value) ? value : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get secret {SecretName}", secretName);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>();
        foreach (var name in secretNames)
        {
            var value = await GetSecretAsync(name, cancellationToken);
            if (value != null)
            {
                result[name] = value;
            }
        }
        return result;
    }
}