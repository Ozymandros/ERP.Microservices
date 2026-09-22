using MyApp.Shared.Domain.Entities;

namespace MyApp.Purchasing.Domain.Entities;

/// <summary>
/// Defines the Purchase Order Status enumeration values.
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>The purchase order has been created but not yet approved.</summary>
    Draft,
    /// <summary>The purchase order has been reviewed and approved for fulfilment.</summary>
    Approved,
    /// <summary>The ordered goods have been received from the supplier.</summary>
    Received,
    /// <summary>The purchase order has been cancelled.</summary>
    Cancelled
}

/// <summary>
/// Purchase order.
/// </summary>
/// <param name="id">The id.</param>
public class PurchaseOrder(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Order Number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets Supplier Id.</summary>
    public Guid SupplierId { get; set; }
    /// <summary>Gets or sets Order Date.</summary>
    public DateTime OrderDate { get; set; }
    /// <summary>Gets or sets Expected Delivery Date.</summary>
    public DateTime? ExpectedDeliveryDate { get; set; }
    /// <summary>Gets or sets Status.</summary>
    public PurchaseOrderStatus Status { get; set; }
    /// <summary>Gets or sets Total Amount.</summary>
    public decimal TotalAmount { get; set; }

    // Receiving tracking
    /// <summary>Gets or sets Receiving Warehouse Id.</summary>
    public Guid? ReceivingWarehouseId { get; set; }
    /// <summary>Gets or sets Received Date.</summary>
    public DateTime? ReceivedDate { get; set; }
    /// <summary>Gets or sets Is Received.</summary>
    public bool IsReceived { get; set; }

    // Navigation
    /// <summary>Gets or sets Supplier.</summary>
    public Supplier? Supplier { get; set; }
    /// <summary>Gets or sets Lines.</summary>
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
}
