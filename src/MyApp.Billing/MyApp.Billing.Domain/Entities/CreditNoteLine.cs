using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// CreditNoteLine entity - represents a single line item on a credit note
/// </summary>
public class CreditNoteLine : AuditableEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the CreditNoteLine class.
    /// </summary>
    /// <param name="creditNoteId">The credit Note Id.</param>
    /// <param name="description">The description.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="taxRate">The tax Rate.</param>
    /// <param name="discount">The discount.</param>
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

    /// <summary>Gets the identifier of the parent credit note.</summary>
    public Guid CreditNoteId { get; private set; }
    /// <summary>Gets the human-readable description of the product or service being credited on this line.</summary>
    public string Description { get; private set; }
    /// <summary>Gets the number of units being credited on this line.</summary>
    public int Quantity { get; private set; }
    /// <summary>Gets the price per unit before tax and discounts.</summary>
    public decimal UnitPrice { get; private set; }
    /// <summary>Gets the flat monetary discount applied to the net amount of this line.</summary>
    public decimal Discount { get; private set; }
    /// <summary>Gets the tax rate for this line, expressed as a percentage (e.g. <c>10</c> for 10%).</summary>
    public decimal TaxRate { get; private set; }

    // Calculated totals
    /// <summary>Gets the net line amount after applying the discount: <c>Quantity * UnitPrice - Discount</c>.</summary>
    public decimal LineNet { get; private set; }
    /// <summary>Gets the tax amount for this line: <c>LineNet * TaxRate / 100</c>.</summary>
    public decimal LineTax { get; private set; }
    /// <summary>Gets the gross line amount including tax: <c>LineNet + LineTax</c>.</summary>
    public decimal LineGross { get; private set; }

    // Navigation
    /// <summary>Gets the parent credit note navigation property.</summary>
    public CreditNote? CreditNote { get; private set; }
}
