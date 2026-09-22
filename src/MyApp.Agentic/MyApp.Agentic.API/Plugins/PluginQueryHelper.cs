using System.Text.Json;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Agentic.API.Plugins;

/// <summary>
/// Shared helpers for building ERP search query strings and normalizing LLM tool arguments.
/// </summary>
internal static class PluginQueryHelper
{
    /// <summary>
    /// Search asynchronously.
    /// </summary>
    /// <param name="serviceInvoker">The service Invoker.</param>
    /// <param name="serviceName">The service Name.</param>
    /// <param name="searchEndpointPath">The search Endpoint Path.</param>
    /// <param name="queryJson">The query Json.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>JSON string containing the search result payload.</returns>
    public static async Task<string> SearchAsync(
        IServiceInvoker serviceInvoker,
        string serviceName,
        string searchEndpointPath,
        string queryJson,
        CancellationToken cancellationToken = default)
    {
        var queryParams = ParseQueryParameters(queryJson);
        var request = serviceInvoker.CreateRequest(
            serviceName,
            searchEndpointPath,
            HttpMethod.Get,
            queryParams: queryParams);

        var result = await serviceInvoker.InvokeAsync<object>(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Parse query parameters.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>Query parameters ready to append to a GET search request.</returns>
    /// <c>name</c>, <c>description</c>, pagination, and sorting properties.
    public static Dictionary<string, string?> ParseQueryParameters(string? args)
    {
        var queryParams = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(args))
            return queryParams;

        var trimmed = args.Trim();
        if (!trimmed.StartsWith('{'))
        {
            queryParams["searchTerm"] = trimmed;
            queryParams["page"] = "1";
            queryParams["pageSize"] = "20";
            return queryParams;
        }

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return queryParams;

        AddIfPresent(queryParams, root, "page");
        AddIfPresent(queryParams, root, "pageSize");
        AddIfPresent(queryParams, root, "sortBy");
        AddIfPresent(queryParams, root, "sortDesc");
        AddIfPresent(queryParams, root, "searchTerm");
        AddIfPresent(queryParams, root, "searchFields");

        if (root.TryGetProperty("filters", out var filters) && filters.ValueKind == JsonValueKind.Object)
        {
            foreach (var filter in filters.EnumerateObject())
            {
                if (!string.IsNullOrWhiteSpace(filter.Name) && filter.Value.ValueKind == JsonValueKind.String)
                    queryParams[filter.Name] = filter.Value.GetString();
            }
        }

        if (!queryParams.ContainsKey("searchTerm")
            && root.TryGetProperty("name", out var nameElement)
            && nameElement.ValueKind == JsonValueKind.String)
        {
            queryParams["name"] = nameElement.GetString();
        }

        if (!queryParams.ContainsKey("searchTerm")
            && root.TryGetProperty("description", out var descriptionElement)
            && descriptionElement.ValueKind == JsonValueKind.String)
        {
            queryParams["description"] = descriptionElement.GetString();
        }

        if (!queryParams.ContainsKey("page"))
            queryParams["page"] = "1";

        if (!queryParams.ContainsKey("pageSize"))
            queryParams["pageSize"] = "20";

        return queryParams;
    }

    /// <summary>
    /// Resolve scalar argument.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <param name="jsonPropertyName">The json Property Name.</param>
    /// <returns>Resolved scalar value suitable for route parameters or exact lookups.</returns>
    public static string ResolveScalarArgument(string? args, string jsonPropertyName = "name")
    {
        if (string.IsNullOrWhiteSpace(args))
            return string.Empty;

        var trimmed = args.Trim().Trim('"');
        if (!trimmed.StartsWith('{'))
            return trimmed;

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return trimmed;

        if (root.TryGetProperty(jsonPropertyName, out var preferred) && preferred.ValueKind == JsonValueKind.String)
            return preferred.GetString() ?? string.Empty;

        if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString() ?? string.Empty;
        }

        return trimmed;
    }

    /// <summary>
    /// Copies a JSON property into the query parameter dictionary when present.
    /// </summary>
    /// <param name="target">Destination query parameter map.</param>
    /// <param name="root">Source JSON object.</param>
    /// <param name="propertyName">Property name to copy.</param>
    private static void AddIfPresent(Dictionary<string, string?> target, JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return;

        target[propertyName] = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}
