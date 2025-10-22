using MyApp.Shared.Domain.Entities;

namespace MyApp.Purchasing.Domain.Entities;

public class Supplier : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
    // Navigation
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
