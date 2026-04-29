using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

/// <summary>
/// Defines the Transaction Type enumeration values.
/// </summary>
public enum TransactionType
{
    Inbound,
    Outbound,
    Adjustment
}

/// <summary>
/// Provides Inventory Transaction functionality.
/// </summary>
public class InventoryTransaction(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets Warehouse Id.</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Gets or sets Quantity Change.</summary>
    public int QuantityChange { get; set; }
    /// <summary>Gets or sets Transaction Type.</summary>
    public TransactionType TransactionType { get; set; }
    /// <summary>Gets or sets Transaction Date.</summary>
    public DateTime TransactionDate { get; set; }

    // Cross-service references
    /// <summary>Gets or sets Order Id.</summary>
    public Guid? OrderId { get; set; }
    /// <summary>Gets or sets Reference Number.</summary>
    public string? ReferenceNumber { get; set; }

    // Navigation
    /// <summary>Gets or sets Product.</summary>
    public Product? Product { get; set; }
    /// <summary>Gets or sets Warehouse.</summary>
    public Warehouse? Warehouse { get; set; }
}
