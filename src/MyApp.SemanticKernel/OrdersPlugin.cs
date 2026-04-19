using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

public class OrdersPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public OrdersPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    [KernelFunction("Create an order")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Post,
            payload);

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Get order by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Orders,
            $"{ApiEndpoints.Orders.Base}/{id}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Update an order")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Put,
            payload);

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Delete an order by id")]
    [Description()]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Orders,
            $"{ApiEndpoints.Orders.Base}/{id}",
            HttpMethod.Delete,
            string.Empty);

        return $"Order {id} deleted successfully.";
    }
}