using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Invoice line entity - represents a single line item on an invoice
/// </summary>
public class InvoiceLine : AuditableEntity<Guid>
{
    public InvoiceLine(Guid invoiceId, string description, int quantity, decimal unitPrice, decimal taxRate, decimal discount = 0)
        : base(Guid.NewGuid())
    {
        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        Discount = discount;

        // Calculate line totals
        LineNet = quantity * unitPrice - discount;
        LineTax = LineNet * (taxRate / 100m);
        LineGross = LineNet + LineTax;
    }

    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal TaxRate { get; private set; }

    // Calculated totals
    public decimal LineNet { get; private set; }
    public decimal LineTax { get; private set; }
    public decimal LineGross { get; private set; }

    // Navigation
    public Invoice? Invoice { get; private set; }
}
