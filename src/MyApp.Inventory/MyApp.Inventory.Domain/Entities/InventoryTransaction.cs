using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

public enum TransactionType
{
    Inbound,
    Outbound,
    Adjustment
}

public class InventoryTransaction(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityChange { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    
    // Cross-service references
    public Guid? OrderId { get; set; }
    public string? ReferenceNumber { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
}
