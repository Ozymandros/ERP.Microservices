using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

public class InventoryPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public InventoryPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    private static string GetBaseEndpoint(string serviceName)
    {
        // Use well-known endpoints when available, fallback to convention api/{service}
        if (serviceName == ServiceNames.Inventory) return ApiEndpoints.Inventory.Base;
        if (serviceName == ServiceNames.Orders) return ApiEndpoints.Orders.Base;
        if (serviceName == ServiceNames.Sales) return ApiEndpoints.Sales.Base;
        return $"api/{serviceName.Replace("-service", string.Empty)}";
    }

    [KernelFunction("Create inventory resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Inventory,
            GetBaseEndpoint(ServiceNames.Inventory),
            HttpMethod.Post,
            payload);

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Get inventory resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"{GetBaseEndpoint(ServiceNames.Inventory)}/{id}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Update inventory resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Inventory,
            GetBaseEndpoint(ServiceNames.Inventory),
            HttpMethod.Put,
            payload);

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Delete inventory resource by id")]
    [Description()]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"{GetBaseEndpoint(ServiceNames.Inventory)}/{id}",
            HttpMethod.Delete,
            string.Empty);

        return $"Inventory resource {id} deleted successfully.";
    }
}
