namespace MyApp.Inventory.Domain.Entities;

public enum TransactionType
{
    Inbound,
    Outbound,
    Adjustment
}

public class InventoryTransaction
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityChange { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    
    // Navigation
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
}
