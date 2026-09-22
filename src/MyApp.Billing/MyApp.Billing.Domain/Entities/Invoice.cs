using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Invoice aggregate root - represents a billing invoice
/// </summary>
public class Invoice : AuditableEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the Invoice class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="customerId">The customer Id.</param>
    /// <param name="currency">The currency.</param>
    public Invoice(Guid id, string invoiceNumber, Guid customerId, string currency) : base(id)
    {
        // Keep materialization/read paths resilient (legacy data may contain empty values).
        // Create/issue flows enforce strict invoice-number validation at application/domain write boundaries.
        InvoiceNumber = invoiceNumber ?? string.Empty;
        CustomerId = customerId;
        Currency = currency;
        Status = InvoiceStatus.Draft;
        Lines = new List<InvoiceLine>();
        Payments = new List<Payment>();
    }

    // Basic info
    /// <summary>Gets the human-readable invoice number assigned at creation or issuance.</summary>
    public string InvoiceNumber { get; private set; }
    /// <summary>Gets the identifier of the customer associated with this invoice.</summary>
    public Guid CustomerId { get; private set; }
    /// <summary>Gets the identifier of the order that originated this invoice, if applicable.</summary>
    public Guid? OrderId { get; private set; }
    /// <summary>Gets the ISO 4217 currency code for this invoice (e.g. <c>"USD"</c>).</summary>
    public string Currency { get; private set; }
    /// <summary>Gets the current lifecycle status of the invoice.</summary>
    public InvoiceStatus Status { get; private set; }

    // Dates
    /// <summary>Gets the date the invoice was issued, or <see langword="null"/> if still in draft.</summary>
    public DateTime? IssueDate { get; private set; }
    /// <summary>Gets the payment due date, calculated from <see cref="IssueDate"/> plus <see cref="PaymentTermsDays"/>.</summary>
    public DateTime? DueDate { get; private set; }

    // Totals
    /// <summary>Gets the total net amount (excluding tax) calculated from all invoice lines.</summary>
    public decimal TotalNet { get; private set; }
    /// <summary>Gets the total tax amount calculated from all invoice lines.</summary>
    public decimal TotalTax { get; private set; }
    /// <summary>Gets the total gross amount (including tax) calculated from all invoice lines.</summary>
    public decimal TotalGross { get; private set; }
    /// <summary>Gets the remaining unpaid amount after all completed payments and credit notes.</summary>
    public decimal OutstandingAmount { get; private set; }

    // Payment terms
    /// <summary>Gets the number of days from the issue date until payment is due.</summary>
    public int PaymentTermsDays { get; private set; } = 30;

    // Navigation
    /// <summary>Gets the collection of line items on this invoice.</summary>
    public List<InvoiceLine> Lines { get; private set; }
    /// <summary>Gets the collection of payments recorded against this invoice.</summary>
    public List<Payment> Payments { get; private set; }
    /// <summary>Gets the collection of credit notes issued against this invoice.</summary>
    public List<CreditNote> CreditNotes { get; private set; } = new();

    /// <summary>
    /// Adds a line.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="taxRate">The tax Rate.</param>
    /// <param name="discount">The discount.</param>
    public void AddLine(string description, int quantity, decimal unitPrice, decimal taxRate, decimal discount = 0)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot modify lines after invoice is issued.");

        var line = new InvoiceLine(Id, description, quantity, unitPrice, taxRate, discount);
        Lines.Add(line);
        RecalculateTotals();
    }

    /// <summary>
    /// Issue sue.
    /// </summary>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="issueDate">The issue Date.</param>
    /// <param name="paymentTermsDays">The payment Terms Days.</param>
    public void Issue(string invoiceNumber, DateTime issueDate, int paymentTermsDays)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be issued.");

        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("InvoiceNumber must be a non-empty unique value.", nameof(invoiceNumber));

        if (Lines.Count == 0)
            throw new InvalidOperationException("Cannot issue invoice without lines.");

        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        PaymentTermsDays = paymentTermsDays;
        DueDate = issueDate.AddDays(paymentTermsDays);
        Status = InvoiceStatus.Issued;
    }

    /// <summary>
    /// Record payment.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <param name="method">The method.</param>
    /// <param name="paidAt">The paid At.</param>
    /// <param name="externalPaymentId">The external Payment Id.</param>
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
    /// Mark as paid.
    /// </summary>
    public void MarkAsPaid()
    {
        if (OutstandingAmount > 0)
            throw new InvalidOperationException("Cannot mark as paid while outstanding amount exists.");

        Status = InvoiceStatus.Paid;
    }

    /// <summary>
    /// Cancel.
    /// </summary>
    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice.");

        Status = InvoiceStatus.Cancelled;
    }

    /// <summary>
    /// Creates a credit note.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <param name="reason">The reason.</param>
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
    /// <summary>The invoice has been created but not yet issued to the customer.</summary>
    Draft,
    /// <summary>The invoice has been issued and is awaiting payment.</summary>
    Issued,
    /// <summary>The invoice has been sent to the customer.</summary>
    Sent,
    /// <summary>The invoice has been fully paid.</summary>
    Paid,
    /// <summary>The invoice has been cancelled and is no longer valid.</summary>
    Cancelled,
    /// <summary>The invoice has been written off as uncollectable.</summary>
    WrittenOff
}
