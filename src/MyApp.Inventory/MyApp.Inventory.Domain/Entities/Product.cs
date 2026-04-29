using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

/// <summary>
/// Provides Product functionality.
/// </summary>
public class Product(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets S K U.</summary>
    public string SKU { get; set; } = string.Empty;
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets Description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets Unit Price.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Gets or sets Quantity In Stock.</summary>
    public int QuantityInStock { get; set; }  // Keep for backward compatibility, will be computed from WarehouseStocks
    /// <summary>Gets or sets Reorder Level.</summary>
    public int ReorderLevel { get; set; }

    // Navigation
    /// <summary>Gets or sets Inventory Transactions.</summary>
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    /// <summary>Gets or sets Warehouse Stocks.</summary>
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = [];
}
