using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

public class CrmPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    public CrmPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    [KernelFunction("Create CRM resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Notification, // fallback; CRM API lives under crm-service but use generic path
            "api/crm/contacts",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Get CRM resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Notification,
            $"api/crm/contacts/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Update CRM resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Notification,
            "api/crm/contacts",
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("Delete CRM resource by id")]
    [Description()]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Notification,
            $"api/crm/contacts/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"CRM contact {id} deleted successfully.";
    }
}
