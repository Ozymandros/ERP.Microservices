using System.Text.Json;
using MyApp.Audit.Application.Contracts.DTOs;

namespace MyApp.Audit.Application.Mapping;

/// <summary>
/// Derives property-level changes by diffing entity JSON snapshots (used for Updated when
/// individual property changes were not captured at commit time).
/// </summary>
public static class SnapshotPropertyChangeDeriver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Builds create DTOs from before/after entity JSON snapshots.</summary>
    public static List<CreatePropertyChangeDto> DeriveCreateDtos(string? originalJson, string? newJson)
    {
        return Derive(originalJson, newJson)
            .Select(p => new CreatePropertyChangeDto
            {
                PropertyName = p.PropertyName,
                OriginalValue = p.OriginalValue,
                NewValue = p.NewValue
            })
            .ToList();
    }

    /// <summary>Builds read DTOs from before/after entity JSON snapshots.</summary>
    public static List<PropertyChangeDto> DeriveReadDtos(string? originalJson, string? newJson)
    {
        return Derive(originalJson, newJson)
            .Select(p => new PropertyChangeDto(
                Guid.Empty,
                p.PropertyName,
                p.OriginalValue,
                p.NewValue))
            .ToList();
    }

    private static List<(string PropertyName, string? OriginalValue, string? NewValue)> Derive(
        string? originalJson,
        string? newJson)
    {
        if (string.IsNullOrWhiteSpace(originalJson) || string.IsNullOrWhiteSpace(newJson))
            return [];

        Dictionary<string, JsonElement>? original;
        Dictionary<string, JsonElement>? updated;
        try
        {
            original = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(originalJson, JsonOptions);
            updated = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(newJson, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (original is null || updated is null)
            return [];

        var propertyNames = original.Keys
            .Union(updated.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        var changes = new List<(string PropertyName, string? OriginalValue, string? NewValue)>();
        foreach (var propertyName in propertyNames)
        {
            original.TryGetValue(propertyName, out var oldElement);
            updated.TryGetValue(propertyName, out var newElement);

            var oldText = FormatValue(oldElement);
            var newText = FormatValue(newElement);
            if (string.Equals(oldText, newText, StringComparison.Ordinal))
                continue;

            changes.Add((propertyName, oldText, newText));
        }

        return changes;
    }

    private static string? FormatValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => element.GetRawText()
        };
    }
}
