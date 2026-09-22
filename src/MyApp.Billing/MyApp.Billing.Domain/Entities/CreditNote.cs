using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Credit note line data.
/// </summary>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="TaxRate">The tax Rate.</param>
/// <param name="Discount">The discount.</param>
public record CreditNoteLineData(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// CreditNote entity - represents a credit note issued against an invoice
/// </summary>
public class CreditNote : AuditableEntity<Guid>
{
    // 1. Add this for EF Core
    private CreditNote() : base(Guid.Empty)
    {
        // EF Core will use this and populate properties via reflection
        Lines = new List<CreditNoteLine>();
        Reason = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the CreditNote class.
    /// building its line items and calculating totals from the supplied line data.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="originalInvoiceId">The original Invoice Id.</param>
    /// <param name="lines">The lines.</param>
    /// <param name="reason">The reason.</param>
    public CreditNote(Guid id, Guid originalInvoiceId, List<CreditNoteLineData> lines, string reason) : base(id)
    {
        OriginalInvoiceId = originalInvoiceId;
        Reason = reason;
        Status = CreditNoteStatus.Issued;
        Lines = new List<CreditNoteLine>();

        foreach (var lineData in lines)
        {
            var line = new CreditNoteLine(
                Id,
                lineData.Description,
                lineData.Quantity,
                lineData.UnitPrice,
                lineData.TaxRate,
                lineData.Discount
            );
            Lines.Add(line);
        }

        // Calculate totals
        TotalNet = Lines.Sum(l => l.LineNet);
        TotalTax = Lines.Sum(l => l.LineTax);
        TotalGross = Lines.Sum(l => l.LineGross);
    }

    /// <summary>Gets the identifier of the invoice this credit note partially or fully reverses.</summary>
    public Guid OriginalInvoiceId { get; private set; }
    /// <summary>Gets the human-readable explanation for issuing this credit note.</summary>
    public string Reason { get; private set; }
    /// <summary>Gets the current lifecycle status of this credit note.</summary>
    public CreditNoteStatus Status { get; private set; }

    // Totals
    /// <summary>Gets the total net amount (excluding tax) across all credit note lines.</summary>
    public decimal TotalNet { get; private set; }
    /// <summary>Gets the total tax amount across all credit note lines.</summary>
    public decimal TotalTax { get; private set; }
    /// <summary>Gets the total gross amount (including tax) across all credit note lines.</summary>
    public decimal TotalGross { get; private set; }

    // Navigation
    /// <summary>Gets the collection of line items that make up this credit note.</summary>
    public List<CreditNoteLine> Lines { get; private set; }
    /// <summary>Gets the navigation property to the original invoice being credited.</summary>
    public Invoice? OriginalInvoice { get; private set; }

    /// <summary>
    /// Cancel.
    /// </summary>
    public void Cancel()
    {
        if (Status == CreditNoteStatus.Applied)
            throw new InvalidOperationException("Cannot cancel an applied credit note.");

        Status = CreditNoteStatus.Cancelled;
    }
}

/// <summary>
/// Credit note status enum
/// </summary>
public enum CreditNoteStatus
{
    /// <summary>The credit note has been created but not yet issued.</summary>
    Draft,
    /// <summary>The credit note has been issued and is available to be applied.</summary>
    Issued,
    /// <summary>The credit note has been applied to reduce the outstanding invoice balance.</summary>
    Applied,
    /// <summary>The credit note has been cancelled and is no longer valid.</summary>
    Cancelled
}
