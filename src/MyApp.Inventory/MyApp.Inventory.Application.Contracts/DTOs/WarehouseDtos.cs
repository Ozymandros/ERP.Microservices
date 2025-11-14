using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record WarehouseDto : AuditableGuidDto
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public record CreateUpdateWarehouseDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(500, MinimumLength = 1)]
    public string Location { get; set; } = string.Empty;
}
