namespace MyApp.Purchasing.Domain.Entities;

/// <summary>
/// Provides Purchase Order Line functionality.
/// </summary>
public class PurchaseOrderLine
{
    /// <summary>Gets or sets Id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets Purchase Order Id.</summary>
    public Guid PurchaseOrderId { get; set; }
    /// <summary>Gets or sets Product Id.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets Quantity.</summary>
    public int Quantity { get; set; }
    /// <summary>Gets or sets Unit Price.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Gets or sets Line Total.</summary>
    public decimal LineTotal { get; set; }

    // Receiving details
    /// <summary>Gets or sets Received Quantity.</summary>
    public int ReceivedQuantity { get; set; }
    /// <summary>Gets or sets Is Fully Received.</summary>
    public bool IsFullyReceived { get; set; }

    // Navigation
    /// <summary>Gets or sets Purchase Order.</summary>
    public PurchaseOrder? PurchaseOrder { get; set; }
}
