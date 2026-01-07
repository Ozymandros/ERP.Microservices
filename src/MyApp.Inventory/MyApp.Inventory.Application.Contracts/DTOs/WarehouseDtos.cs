using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record WarehouseDto(Guid Id) : AuditableGuidDto(Id)
{
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}

public record CreateUpdateWarehouseDto(
    [property: Required(ErrorMessage = "Name is required")]
    [property: StringLength(255, MinimumLength = 1)]
    string Name,

    [property: Required(ErrorMessage = "Location is required")]
    [property: StringLength(500, MinimumLength = 1)]
    string Location = ""
);
