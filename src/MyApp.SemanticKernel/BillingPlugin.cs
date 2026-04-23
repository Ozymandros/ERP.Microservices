using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

public class BillingPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public BillingPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

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
