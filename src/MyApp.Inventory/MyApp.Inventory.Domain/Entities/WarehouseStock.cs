using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

public class WarehouseStock(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int OnOrderQuantity { get; set; }
    
    // Navigation properties
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
    
    // Computed property
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;
}
