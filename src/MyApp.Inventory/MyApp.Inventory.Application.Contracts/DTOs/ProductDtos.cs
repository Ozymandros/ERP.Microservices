using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record ProductDto(Guid Id) : AuditableGuidDto(Id)
{
    public string SKU { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; } = 0;
    public int QuantityInStock { get; init; } = 0;
    public int ReorderLevel { get; init; } = 0;
}

public record CreateUpdateProductDto(
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(64, MinimumLength = 1)]
    string SKU,

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string Description = "",

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    decimal UnitPrice = 0,

    [Range(0, int.MaxValue, ErrorMessage = "QuantityInStock must be greater than or equal to 0")]
    int QuantityInStock = 0,

    [Range(0, int.MaxValue, ErrorMessage = "ReorderLevel must be greater than or equal to 0")]
    int ReorderLevel = 0
);
