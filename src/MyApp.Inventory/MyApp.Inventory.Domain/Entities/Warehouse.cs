using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

/// <summary>
/// Provides Warehouse functionality.
/// </summary>
public class Warehouse(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets Location.</summary>
    public string Location { get; set; } = string.Empty;

    // Navigation
    /// <summary>Gets or sets Inventory Transactions.</summary>
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
}
