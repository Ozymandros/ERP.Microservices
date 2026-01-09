namespace MyApp.Purchasing.Domain.Entities;

public class PurchaseOrderLine
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    
    // Receiving details
    public int ReceivedQuantity { get; set; }
    public bool IsFullyReceived { get; set; }
    
    // Navigation
    public PurchaseOrder? PurchaseOrder { get; set; }
}
