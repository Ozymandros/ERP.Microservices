using System.ComponentModel.DataAnnotations;
using MyApp.Audit.Domain;

namespace MyApp.Audit.Application.Contracts.DTOs;

/// <summary>Read model for a property-level audit entry.</summary>
/// <param name="Id">The id.</param>
/// <param name="PropertyName">The property Name.</param>
/// <param name="OriginalValue">The original Value.</param>
/// <param name="NewValue">The new Value.</param>
public record PropertyChangeDto(
    Guid Id,
    string PropertyName,
    string? OriginalValue,
    string? NewValue);

/// <summary>Read model for an entity audit record including property changes.</summary>
/// <param name="Id">The id.</param>
/// <param name="EntityName">The entity Name.</param>
/// <param name="EntityId">The entity Id.</param>
/// <param name="ChangeType">The change Type.</param>
/// <param name="OriginalValue">The original Value.</param>
/// <param name="NewValue">The new Value.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="CreatedBy">The created By.</param>
/// <param name="UpdatedAt">The updated At.</param>
/// <param name="UpdatedBy">The updated By.</param>
/// <param name="PropertyChanges">The property Changes.</param>
public record EntityChangeDto(
    Guid Id,
    string EntityName,
    Guid EntityId,
    string ChangeType,
    string? OriginalValue,
    string? NewValue,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<PropertyChangeDto> PropertyChanges);

/// <summary>DTO for recording a single property change within an entity change.</summary>
public record CreatePropertyChangeDto
{
    [Required(ErrorMessage = "PropertyName is required")]
    [StringLength(200, MinimumLength = 1)]
    public string PropertyName { get; init; } = string.Empty;

    public string? OriginalValue { get; init; }

    public string? NewValue { get; init; }
}

/// <summary>DTO for appending a new entity change to the audit trail.</summary>
public record CreateEntityChangeDto
{
    [Required(ErrorMessage = "EntityName is required")]
    [StringLength(200, MinimumLength = 1)]
    public string EntityName { get; init; } = string.Empty;

    [Required]
    public Guid EntityId { get; init; }

    [Required]
    public ChangeTypeEnum ChangeType { get; init; }

    public string? OriginalValue { get; init; }

    public string? NewValue { get; init; }

    public List<CreatePropertyChangeDto> PropertyChanges { get; init; } = [];
}
