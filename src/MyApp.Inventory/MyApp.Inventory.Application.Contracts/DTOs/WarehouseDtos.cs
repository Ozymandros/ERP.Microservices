using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

/// <summary>
/// Warehouse dto.
/// </summary>
/// <param name="Id">The id.</param>
public record WarehouseDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets or sets Location.</summary>
    public string Location { get; init; } = string.Empty;
}

/// <summary>
/// Creates an update warehouse dto.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Location">The location.</param>
public record CreateUpdateWarehouseDto(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1)]
    string Name,

    [Required(ErrorMessage = "Location is required")]
    [StringLength(500, MinimumLength = 1)]
    string Location = ""
);
