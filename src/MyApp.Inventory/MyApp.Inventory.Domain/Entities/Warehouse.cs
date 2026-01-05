using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

public class Warehouse(Guid id) : AuditableEntity<Guid>(id)
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    // Navigation
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
}
