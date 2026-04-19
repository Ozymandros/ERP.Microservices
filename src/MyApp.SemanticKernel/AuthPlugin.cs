using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.Text.Json;

public class AuthPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public AuthPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    [KernelFunction("Authenticate user / login")]
    public async Task<string> LoginAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Auth,
            "api/auth/login",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Get user by id")]
    public async Task<string> GetUserAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Auth,
            $"api/auth/users/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }
}
