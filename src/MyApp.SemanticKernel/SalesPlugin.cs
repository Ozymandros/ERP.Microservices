using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

public class SalesPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public SalesPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    [KernelFunction("Create sales order")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Sales,
            ApiEndpoints.Sales.Base,
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Get sales order by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"{ApiEndpoints.Sales.Base}/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Update sales order")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Sales,
            ApiEndpoints.Sales.Base,
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Delete sales order by id")]
    [Description()]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"{ApiEndpoints.Sales.Base}/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"Sales order {id} deleted successfully.";
    }
}
