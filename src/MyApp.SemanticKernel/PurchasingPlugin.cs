using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

public class PurchasingPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public PurchasingPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    [KernelFunction("Create purchasing resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Purchasing,
            "api/purchasing/purchaseorders",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Get purchasing resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Purchasing,
            $"api/purchasing/purchaseorders/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Update purchasing resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Purchasing,
            "api/purchasing/purchaseorders",
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Delete purchasing resource by id")]
    [Description("Deletes a purchasing resource by its identifier")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Purchasing,
            $"api/purchasing/purchaseorders/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"Purchasing resource {id} deleted successfully.";
    }
}
