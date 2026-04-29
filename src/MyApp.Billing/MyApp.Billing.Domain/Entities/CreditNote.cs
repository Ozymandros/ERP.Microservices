using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Data transfer object for credit note line creation
/// </summary>
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

    // 2. Keep your domain constructor exactly as is
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

    public Guid OriginalInvoiceId { get; private set; }
    public string Reason { get; private set; }
    public CreditNoteStatus Status { get; private set; }

    // Totals
    public decimal TotalNet { get; private set; }
    public decimal TotalTax { get; private set; }
    public decimal TotalGross { get; private set; }

    // Navigation
    public List<CreditNoteLine> Lines { get; private set; }
    public Invoice? OriginalInvoice { get; private set; }

    /// <summary>
    /// Cancels the credit note
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
    Draft,
    Issued,
    Applied,
    Cancelled
}
