using MyApp.Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public class InventoryTransactionDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityChange { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public ProductDto? Product { get; set; }
    public WarehouseDto? Warehouse { get; set; }
}

public class CreateUpdateInventoryTransactionDto
{
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
