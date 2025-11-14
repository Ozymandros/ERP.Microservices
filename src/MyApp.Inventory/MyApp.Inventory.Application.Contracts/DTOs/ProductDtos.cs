using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record ProductDto : AuditableGuidDto
{
    public Guid Id { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int QuantityInStock { get; init; }
    public int ReorderLevel { get; init; }
}

public record CreateUpdateProductDto(
    [property: Required(ErrorMessage = "SKU is required")]
    [property: StringLength(64, MinimumLength = 1)]
    string SKU,

    [property: Required(ErrorMessage = "Name is required")]
    [property: StringLength(255, MinimumLength = 1)]
    string Name,

    [property: StringLength(1000)]
    string Description,

    [property: Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    decimal UnitPrice,

    [property: Range(0, int.MaxValue, ErrorMessage = "QuantityInStock must be greater than or equal to 0")]
    int QuantityInStock,

    [property: Range(0, int.MaxValue, ErrorMessage = "ReorderLevel must be greater than or equal to 0")]
    int ReorderLevel
);
