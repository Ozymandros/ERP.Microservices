using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Invoice aggregate root - represents a billing invoice
/// </summary>
public class Invoice : AuditableEntity<Guid>
{
    public Invoice(Guid id, Guid customerId, string currency) : base(id)
    {
        CustomerId = customerId;
        Currency = currency;
        Status = InvoiceStatus.Draft;
        Lines = new List<InvoiceLine>();
        Payments = new List<Payment>();
    }

    // Basic info
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? OrderId { get; private set; }
    public string Currency { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Dates
    public DateTime? IssueDate { get; private set; }
    public DateTime? DueDate { get; private set; }

    // Totals
    public decimal TotalNet { get; private set; }
    public decimal TotalTax { get; private set; }
    public decimal TotalGross { get; private set; }
    public decimal OutstandingAmount { get; private set; }

    // Payment terms
    public int PaymentTermsDays { get; private set; } = 30;

    // Navigation
    public List<InvoiceLine> Lines { get; private set; }
    public List<Payment> Payments { get; private set; }
    public List<CreditNote> CreditNotes { get; private set; } = new();

    /// <summary>
    /// Adds a line to the invoice (only allowed in Draft status)
    /// </summary>
    public void AddLine(string description, int quantity, decimal unitPrice, decimal taxRate, decimal discount = 0)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot modify lines after invoice is issued.");

        var line = new InvoiceLine(Id, description, quantity, unitPrice, taxRate, discount);
        Lines.Add(line);
        RecalculateTotals();
    }

    /// <summary>
    /// Issues the invoice - locks lines and assigns invoice number
    /// </summary>
    public void Issue(string invoiceNumber, DateTime issueDate, int paymentTermsDays)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be issued.");

        if (Lines.Count == 0)
            throw new InvalidOperationException("Cannot issue invoice without lines.");

        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        PaymentTermsDays = paymentTermsDays;
        DueDate = issueDate.AddDays(paymentTermsDays);
        Status = InvoiceStatus.Issued;
    }

    /// <summary>
    /// Records a payment against the invoice
    /// </summary>
    public void RecordPayment(decimal amount, string method, DateTime paidAt, string? externalPaymentId = null)
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Cannot record payment on cancelled invoice.");

        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));

        var payment = new Payment(Guid.NewGuid(), Id, amount, Currency, method, paidAt, externalPaymentId);
        Payments.Add(payment);
        RecalculateOutstanding();
    }

    /// <summary>
    /// Marks invoice as paid if outstanding is zero
    /// </summary>
    public void MarkAsPaid()
    {
        if (OutstandingAmount > 0)
            throw new InvalidOperationException("Cannot mark as paid while outstanding amount exists.");

        Status = InvoiceStatus.Paid;
    }

    /// <summary>
    /// Cancels the invoice
    /// </summary>
    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice.");

        Status = InvoiceStatus.Cancelled;
    }

    /// <summary>
    /// Creates a credit note for this invoice
    /// </summary>
    public CreditNote CreateCreditNote(List<CreditNoteLineData> lines, string reason)
    {
        if (Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot create credit note for draft invoice.");

        var creditNote = new CreditNote(Guid.NewGuid(), Id, lines, reason);
        CreditNotes.Add(creditNote);
        
        // Adjust outstanding amount
        foreach (var line in lines)
        {
            var lineGross = line.Quantity * line.UnitPrice * (1 + line.TaxRate / 100m) - line.Discount;
            OutstandingAmount -= lineGross;
        }

        return creditNote;
    }

    /// <summary>
    /// Recalculates all totals from lines
    /// </summary>
    private void RecalculateTotals()
    {
        TotalNet = Lines.Sum(l => l.LineNet);
        TotalTax = Lines.Sum(l => l.LineTax);
        TotalGross = Lines.Sum(l => l.LineGross);
        RecalculateOutstanding();
    }

    /// <summary>
    /// Recalculates outstanding amount from payments
    /// </summary>
    private void RecalculateOutstanding()
    {
        var totalPaid = Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
        OutstandingAmount = TotalGross - totalPaid;

        if (OutstandingAmount == 0 && Status == InvoiceStatus.Issued)
        {
            Status = InvoiceStatus.Paid;
        }
    }
}

/// <summary>
/// Invoice status enum
/// </summary>
public enum InvoiceStatus
{
    Draft,
    Issued,
    Sent,
    Paid,
    Cancelled,
    WrittenOff
}
