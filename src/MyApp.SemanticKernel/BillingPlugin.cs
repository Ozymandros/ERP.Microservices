using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Semantic Kernel plugin for billing operations including invoice and payment management.
/// </summary>
public class BillingPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of the BillingPlugin with the required service invoker.
    /// </summary>
    public BillingPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new billing resource such as an invoice or payment record.
    /// </summary>
    [KernelFunction("Create billing resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Billing,
            "api/billing",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves a billing resource by its unique identifier.
    /// </summary>
    [KernelFunction("Get billing resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Billing,
            $"api/billing/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing billing resource with new information.
    /// </summary>
    [KernelFunction("Update billing resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Billing,
            "api/billing",
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes a billing resource (invoice or payment record) by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the billing resource to delete.</param>
    /// <returns>A confirmation message indicating the billing resource was deleted.</returns>
    [KernelFunction("Delete billing resource by id")]
    [Description("Deletes a billing resource by its identifier")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Billing,
            $"api/billing/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"Billing resource {id} deleted successfully.";
    }
}
