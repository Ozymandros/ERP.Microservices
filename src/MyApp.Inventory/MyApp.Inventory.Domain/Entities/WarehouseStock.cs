using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

/// <summary>
/// Warehouse stock.
/// </summary>
/// <param name="id">The id.</param>
public class WarehouseStock(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets Warehouse Id.</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Gets or sets Available Quantity.</summary>
    public int AvailableQuantity { get; set; }
    /// <summary>Gets or sets Reserved Quantity.</summary>
    public int ReservedQuantity { get; set; }
    /// <summary>Gets or sets On Order Quantity.</summary>
    public int OnOrderQuantity { get; set; }

    // Navigation properties
    /// <summary>Gets or sets Product.</summary>
    public Product? Product { get; set; }
    /// <summary>Gets or sets Warehouse.</summary>
    public Warehouse? Warehouse { get; set; }

    // Computed property
    /// <summary>Gets the total quantity of items including both available and reserved stock.</summary>
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;
}
