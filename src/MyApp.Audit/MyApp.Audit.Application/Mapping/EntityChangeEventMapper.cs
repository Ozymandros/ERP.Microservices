using System.Text.Json;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Events;

namespace MyApp.Audit.Application.Mapping;

/// <summary>Maps cross-service audit events into Audit application DTOs.</summary>
public static class EntityChangeEventMapper
{
    /// <summary>Maps a single payload to <see cref="CreateEntityChangeDto"/>.</summary>
    public static CreateEntityChangeDto? ToCreateDto(EntityChangePayload payload)
    {
        var entityId = ResolveEntityId(payload.EntityId);
        if (entityId is null)
            return null;

        var changeType = payload.State switch
        {
            "Added" => ChangeTypeEnum.Created,
            "Modified" => ChangeTypeEnum.Updated,
            "Deleted" => ChangeTypeEnum.Deleted,
            _ => (ChangeTypeEnum?)null
        };

        if (changeType is null)
            return null;

        var propertyChanges = payload.Properties
            .Select(p => new CreatePropertyChangeDto
            {
                PropertyName = p.PropertyName,
                OriginalValue = p.OldValue?.ToString(),
                NewValue = p.NewValue?.ToString()
            })
            .ToList();

        if (changeType == ChangeTypeEnum.Updated
            && propertyChanges.Count == 0
            && !string.IsNullOrWhiteSpace(payload.OriginalValue)
            && !string.IsNullOrWhiteSpace(payload.NewValue))
        {
            propertyChanges = SnapshotPropertyChangeDeriver.DeriveCreateDtos(
                payload.OriginalValue,
                payload.NewValue);
        }

        return new CreateEntityChangeDto
        {
            EntityName = payload.EntityName,
            EntityId = entityId.Value,
            ChangeType = changeType.Value,
            OriginalValue = changeType == ChangeTypeEnum.Created ? null : payload.OriginalValue,
            NewValue = changeType == ChangeTypeEnum.Deleted ? null : payload.NewValue,
            PropertyChanges = propertyChanges
        };
    }

    private static Guid? ResolveEntityId(object? entityId) => entityId switch
    {
        Guid guid when guid != Guid.Empty => guid,
        string s when Guid.TryParse(s, out var parsed) && parsed != Guid.Empty => parsed,
        JsonElement json when json.ValueKind == JsonValueKind.String
            && Guid.TryParse(json.GetString(), out var parsed) && parsed != Guid.Empty => parsed,
        _ => null
    };
}
