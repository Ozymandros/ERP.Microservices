namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Published after a microservice commits entity changes. Consumed only by the Audit module.
/// </summary>
/// <param name="SourceService">Dapr app-id of the service that produced the changes.</param>
/// <param name="Changes">Entity change payloads captured at commit time.</param>
public sealed record EntityChangesSavedEvent(
    string SourceService,
    IReadOnlyList<EntityChangePayload> Changes);

/// <summary>
/// One entity change within <see cref="EntityChangesSavedEvent"/>.
/// </summary>
public sealed record EntityChangePayload(
    string EntityName,
    object? EntityId,
    string State,
    IReadOnlyList<PropertyChangePayload> Properties,
    string? OriginalValue = null,
    string? NewValue = null);

/// <summary>
/// A single property change within <see cref="EntityChangePayload"/>.
/// </summary>
public sealed record PropertyChangePayload(
    string PropertyName,
    object? OldValue,
    object? NewValue);
