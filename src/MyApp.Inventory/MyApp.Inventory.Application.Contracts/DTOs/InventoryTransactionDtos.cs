using MyApp.Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

/// <summary>
/// Inventory transaction dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="QuantityChange">The quantity Change.</param>
/// <param name="TransactionType">The transaction Type.</param>
/// <param name="TransactionDate">The transaction Date.</param>
/// <param name="Product">The product.</param>
/// <param name="Warehouse">The warehouse.</param>
/// <param name="ReferenceNumber">The reference Number.</param>
public record InventoryTransactionDto(
    Guid Id,
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    TransactionType TransactionType,
    DateTime TransactionDate,
    ProductDto? Product = null,
    WarehouseDto? Warehouse = null,
    string? ReferenceNumber = null
);

/// <summary>
/// Creates an update inventory transaction dto.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="QuantityChange">The quantity Change.</param>
/// <param name="TransactionType">The transaction Type.</param>
/// <param name="TransactionDate">The transaction Date.</param>
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
