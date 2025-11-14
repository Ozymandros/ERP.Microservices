using MyApp.Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record InventoryTransactionDto(
    Guid Id,
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    TransactionType TransactionType,
    DateTime TransactionDate,
    ProductDto? Product = null,
    WarehouseDto? Warehouse = null
);

public record CreateUpdateInventoryTransactionDto(
    [property: Required(ErrorMessage = "ProductId is required")]
    Guid ProductId,

    [property: Required(ErrorMessage = "WarehouseId is required")]
    Guid WarehouseId,

    [property: Required(ErrorMessage = "QuantityChange is required")]
    [property: Range(-1000000, 1000000, ErrorMessage = "QuantityChange must be between -1000000 and 1000000")]
    int QuantityChange,

    [property: Required(ErrorMessage = "TransactionType is required")]
    TransactionType TransactionType,

    DateTime TransactionDate
);
