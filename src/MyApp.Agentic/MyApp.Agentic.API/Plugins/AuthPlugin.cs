using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Semantic Kernel plugin for user authentication and authorization operations.
/// </summary>
public class AuthPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of the AuthPlugin with the required service invoker.
    /// </summary>
    /// <param name="serviceInvoker">The inter-service HTTP invoker used to call the Auth service.</param>
    public AuthPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Authenticates a user and returns login credentials or session information.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized login request containing credentials.</param>
    /// <returns>JSON representation of the authentication result.</returns>
    [Description("Authenticate user / login")]
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

    /// <summary>
    /// Retrieves user information by user identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>JSON representation of the user, or an error description if not found.</returns>
    [Description("Get user by id")]
    public async Task<string> GetUserAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Auth,
            $"api/users/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }
}
