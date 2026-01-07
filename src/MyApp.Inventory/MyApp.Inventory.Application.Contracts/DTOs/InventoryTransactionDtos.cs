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
    [Required(ErrorMessage = "ProductId is required")]
    Guid ProductId,

    [Required(ErrorMessage = "WarehouseId is required")]
    Guid WarehouseId,

    [Required(ErrorMessage = "QuantityChange is required")]
    [Range(-1000000, 1000000, ErrorMessage = "QuantityChange must be between -1000000 and 1000000")]
    int QuantityChange,

    [Required(ErrorMessage = "TransactionType is required")]
    TransactionType TransactionType,

    DateTime TransactionDate
);
