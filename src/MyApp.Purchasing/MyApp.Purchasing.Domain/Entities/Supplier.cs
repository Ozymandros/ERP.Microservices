using MyApp.Shared.Domain.Entities;

namespace MyApp.Purchasing.Domain.Entities;

/// <summary>
/// Provides Supplier functionality.
/// </summary>
public class Supplier(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets Contact Name.</summary>
    public string ContactName { get; set; } = string.Empty;
    /// <summary>Gets or sets Email.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets Phone Number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets Address.</summary>
    public string Address { get; set; } = string.Empty;

    // Navigation
    /// <summary>Gets or sets Purchase Orders.</summary>
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
