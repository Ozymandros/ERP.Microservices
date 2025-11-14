using MyApp.Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public record InventoryTransactionDto
{
    public InventoryTransactionDto() { }
    
    public InventoryTransactionDto(
        Guid id,
        Guid productId,
        Guid warehouseId,
        int quantityChange,
        TransactionType transactionType,
        DateTime transactionDate,
        ProductDto? product = null,
        WarehouseDto? warehouse = null)
    {
        Id = id;
        ProductId = productId;
        WarehouseId = warehouseId;
        QuantityChange = quantityChange;
        TransactionType = transactionType;
        TransactionDate = transactionDate;
        Product = product;
        Warehouse = warehouse;
    }
    
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityChange { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public ProductDto? Product { get; set; }
    public WarehouseDto? Warehouse { get; set; }
}

public record CreateUpdateInventoryTransactionDto
{
    public CreateUpdateInventoryTransactionDto() { }
    
    public CreateUpdateInventoryTransactionDto(
        Guid productId,
        Guid warehouseId,
        int quantityChange,
        TransactionType transactionType,
        DateTime transactionDate)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        QuantityChange = quantityChange;
        TransactionType = transactionType;
        TransactionDate = transactionDate;
    }
    
    [Required(ErrorMessage = "ProductId is required")]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "WarehouseId is required")]
    public Guid WarehouseId { get; set; }

    [Required(ErrorMessage = "QuantityChange is required")]
    [Range(-1000000, 1000000, ErrorMessage = "QuantityChange must be between -1000000 and 1000000")]
    public int QuantityChange { get; set; }

    [Required(ErrorMessage = "TransactionType is required")]
    public TransactionType TransactionType { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
