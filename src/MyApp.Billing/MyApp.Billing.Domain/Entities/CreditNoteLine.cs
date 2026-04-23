using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// CreditNoteLine entity - represents a single line item on a credit note
/// </summary>
public class CreditNoteLine : AuditableEntity<Guid>
{
    public CreditNoteLine(Guid creditNoteId, string description, int quantity, decimal unitPrice, decimal taxRate, decimal discount = 0)
        : base(Guid.NewGuid())
    {
        CreditNoteId = creditNoteId;
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

    public Guid CreditNoteId { get; private set; }
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
    public CreditNote? CreditNote { get; private set; }
}
