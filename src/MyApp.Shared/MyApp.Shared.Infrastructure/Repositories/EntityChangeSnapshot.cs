using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyApp.Shared.Domain.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyApp.Shared.Infrastructure.Repositories;

/// <summary>
/// Builds <see cref="EntityEntryDto"/> snapshots from an EF Core change tracker before commit.
/// </summary>
public static class EntityChangeSnapshot
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Captures pending changes, persists them, and returns commit summaries.
    /// </summary>
    public static async Task<IReadOnlyCollection<EntityEntryDto>> CommitAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var entries = dbContext.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var snapshots = entries
            .Select(entry =>
            {
                var (originalJson, newJson) = ResolveEntitySnapshots(entry);
                return new
                {
                    Entry = entry,
                    EntityName = entry.Metadata.ClrType.Name,
                    State = entry.State.ToString(),
                    Properties = GetPropertyChanges(entry),
                    OriginalValue = originalJson,
                    NewValue = newJson
                };
            })
            .ToList();

        await dbContext.SaveChangesAsync(cancellationToken);

        return snapshots
            .Select(s => new EntityEntryDto(
                s.EntityName,
                ResolvePrimaryKey(s.Entry),
                s.State,
                s.Properties,
                s.OriginalValue,
                s.NewValue))
            .ToList();
    }

    private static (string? OriginalValue, string? NewValue) ResolveEntitySnapshots(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => (null, BuildEntitySnapshotJson(entry, useOriginalValues: false)),
            EntityState.Deleted => (BuildEntitySnapshotJson(entry, useOriginalValues: true), null),
            EntityState.Modified => (
                BuildEntitySnapshotJson(entry, useOriginalValues: true),
                BuildEntitySnapshotJson(entry, useOriginalValues: false)),
            _ => (null, null)
        };
    }

    private static string? BuildEntitySnapshotJson(EntityEntry entry, bool useOriginalValues)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
                continue;

            dict[property.Metadata.Name] = useOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;
        }

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict, SnapshotJsonOptions);
    }

    private static object? ResolvePrimaryKey(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
            return null;

        var keyProperties = primaryKey.Properties;
        if (keyProperties.Count == 0)
            return null;

        if (keyProperties.Count == 1)
            return entry.Property(keyProperties[0].Name).CurrentValue;

        var parts = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);
        return string.Join("|", parts);
    }

    private static IReadOnlyCollection<PropertyChangeEntryDto> GetPropertyChanges(EntityEntry entry)
    {
        var properties = new List<PropertyChangeEntryDto>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    properties.Add(new PropertyChangeEntryDto(
                        property.Metadata.Name,
                        null,
                        property.CurrentValue));
                    break;

                case EntityState.Deleted:
                    properties.Add(new PropertyChangeEntryDto(
                        property.Metadata.Name,
                        property.OriginalValue,
                        null));
                    break;

                case EntityState.Modified:
                    var oldValue = property.OriginalValue;
                    var newValue = property.CurrentValue;

                    if (Equals(oldValue, newValue))
                        continue;

                    properties.Add(new PropertyChangeEntryDto(
                        property.Metadata.Name,
                        oldValue,
                        newValue));
                    break;
            }
        }

        return properties;
    }
}
