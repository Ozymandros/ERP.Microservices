namespace MyApp.Inventory.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    
    // Navigation
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
}
