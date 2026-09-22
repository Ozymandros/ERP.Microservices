namespace MyApp.Shared.Domain.Repositories;

/// <summary>
/// Property change entry dto.
/// </summary>
/// <param name="PropertyName">The property Name.</param>
/// <param name="OldValue">The old Value.</param>
/// <param name="NewValue">The new Value.</param>
public sealed record PropertyChangeEntryDto(
    string PropertyName,
    object? OldValue,
    object? NewValue);

/// <summary>
/// Entity entry dto.
/// </summary>
/// <param name="EntityName">The entity Name.</param>
/// <param name="EntityId">The entity Id.</param>
/// <param name="State">The state.</param>
/// <param name="Properties">The properties.</param>
/// <param name="OriginalValue">The original Value.</param>
/// <param name="NewValue">The new Value.</param>
public sealed record EntityEntryDto(
    string EntityName,
    object? EntityId,
    string State,
    IReadOnlyCollection<PropertyChangeEntryDto> Properties,
    string? OriginalValue = null,
    string? NewValue = null);
