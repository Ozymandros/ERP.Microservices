namespace MyApp.Purchasing.Domain.Entities;

public enum PurchaseOrderStatus
{
    Draft,
    Approved,
    Received,
    Cancelled
}

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Navigation
    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
}
