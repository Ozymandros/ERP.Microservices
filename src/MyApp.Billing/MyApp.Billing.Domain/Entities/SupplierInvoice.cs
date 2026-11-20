using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>Supplier (AP) invoice linked to a purchase order for 3-way match.</summary>
public class SupplierInvoice(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the supplier invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the supplier identifier.</summary>
    public Guid SupplierId { get; set; }
    /// <summary>Gets or sets the purchase order identifier.</summary>
    public Guid PurchaseOrderId { get; set; }
    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "USD";
    /// <summary>Gets or sets line items.</summary>
    public List<SupplierInvoiceLine> Lines { get; set; } = [];
}

/// <summary>Line on a supplier invoice.</summary>
public class SupplierInvoiceLine(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the parent supplier invoice id.</summary>
    public Guid SupplierInvoiceId { get; set; }
    /// <summary>Gets or sets the product id.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets billed quantity.</summary>
    public int Quantity { get; set; }
    /// <summary>Gets or sets unit price.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Navigation to parent invoice.</summary>
    public SupplierInvoice? SupplierInvoice { get; set; }
}
