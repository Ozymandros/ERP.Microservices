namespace MyApp.Inventory.Domain.Entities;

public class Warehouse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Navigation
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
}
