using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

namespace MyApp.Agentic.API.Plugins;

/// <summary>
/// Semantic Kernel plugin for user authentication and authorization operations.
/// </summary>
public class AuthPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of the AuthPlugin class.
    /// </summary>
    /// <param name="serviceInvoker">The service Invoker.</param>
    public AuthPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Authenticates a user and returns login credentials or session information.
    /// </summary>
    /// <param name="payloadJson">The payload Json.</param>
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
    /// <param name="id">The id.</param>
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
