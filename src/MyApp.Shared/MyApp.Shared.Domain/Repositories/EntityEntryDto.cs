namespace MyApp.Shared.Domain.Repositories;

/// <summary>
/// Describes a single property change on an entity before persistence.
/// </summary>
/// <param name="PropertyName">The CLR property name.</param>
/// <param name="OldValue">The original value, or <see langword="null"/> for inserts.</param>
/// <param name="NewValue">The new value, or <see langword="null"/> for deletes.</param>
public sealed record PropertyChangeEntryDto(
    string PropertyName,
    object? OldValue,
    object? NewValue);

/// <summary>
/// Summarizes pending changes for one tracked entity captured at <see cref="IUnitOfWork.CommitAsync"/> time.
/// </summary>
/// <param name="EntityName">The CLR type name of the entity.</param>
/// <param name="EntityId">
/// The entity's primary key value, captured after save. <see langword="null"/> when the entity
/// has no primary key or the key value could not be resolved.
/// </param>
/// <param name="State">The EF entity state name (Added, Modified, or Deleted).</param>
/// <param name="Properties">Property-level old/new values for the change.</param>
/// <param name="OriginalValue">
/// JSON snapshot of the whole entity before the change. <see langword="null"/> for Added (Created).
/// </param>
/// <param name="NewValue">
/// JSON snapshot of the whole entity after the change. <see langword="null"/> for Deleted.
/// </param>
public sealed record EntityEntryDto(
    string EntityName,
    object? EntityId,
    string State,
    IReadOnlyCollection<PropertyChangeEntryDto> Properties,
    string? OriginalValue = null,
    string? NewValue = null);
