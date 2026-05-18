using System.ComponentModel.DataAnnotations;
using MyApp.Audit.Domain;

namespace MyApp.Audit.Application.Contracts.DTOs;

/// <summary>Read model for a property-level audit entry.</summary>
public record PropertyChangeDto(
    Guid Id,
    string PropertyName,
    string? OriginalValue,
    string? NewValue);

/// <summary>Read model for an entity audit record including property changes.</summary>
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
