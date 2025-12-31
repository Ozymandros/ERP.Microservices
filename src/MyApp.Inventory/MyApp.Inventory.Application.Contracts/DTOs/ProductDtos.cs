using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record ProductDto(
    Guid Id,
    DateTime CreatedAt = default,
    string CreatedBy = "",
    DateTime? UpdatedAt = null,
    string? UpdatedBy = null,
    string SKU = "",
    string Name = "",
    string Description = "",
    decimal UnitPrice = 0,
    int QuantityInStock = 0,
    int ReorderLevel = 0
) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

public record CreateUpdateProductDto(
    [property: Required(ErrorMessage = "SKU is required")]
    [property: StringLength(64, MinimumLength = 1)]
    string SKU,

    [property: Required(ErrorMessage = "Name is required")]
    [property: StringLength(255, MinimumLength = 1)]
    string Name,

    [property: StringLength(1000)]
    string Description = "",

    [property: Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    decimal UnitPrice = 0,

    [property: Range(0, int.MaxValue, ErrorMessage = "QuantityInStock must be greater than or equal to 0")]
    int QuantityInStock = 0,

    [property: Range(0, int.MaxValue, ErrorMessage = "ReorderLevel must be greater than or equal to 0")]
    int ReorderLevel = 0
);
