namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Entity changes saved event.
/// </summary>
/// <param name="SourceService">The source Service.</param>
/// <param name="Changes">The changes.</param>
public sealed record EntityChangesSavedEvent(
    string SourceService,
    IReadOnlyList<EntityChangePayload> Changes);

/// <summary>
/// Entity change payload.
/// </summary>
/// <param name="EntityName">The entity Name.</param>
/// <param name="EntityId">The entity Id.</param>
/// <param name="State">The state.</param>
/// <param name="Properties">The properties.</param>
/// <param name="OriginalValue">The original Value.</param>
/// <param name="NewValue">The new Value.</param>
public sealed record EntityChangePayload(
    string EntityName,
    object? EntityId,
    string State,
    IReadOnlyList<PropertyChangePayload> Properties,
    string? OriginalValue = null,
    string? NewValue = null);

/// <summary>
/// Property change payload.
/// </summary>
/// <param name="PropertyName">The property Name.</param>
/// <param name="OldValue">The old Value.</param>
/// <param name="NewValue">The new Value.</param>
public sealed record PropertyChangePayload(
    string PropertyName,
    object? OldValue,
    object? NewValue);
