using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Application.Commands;

/// <summary>
/// Creates an invoice command.
/// </summary>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The order Id.</param>
/// <param name="Currency">The currency.</param>
/// <param name="Lines">The lines.</param>
/// <param name="PaymentTermsDays">The payment Terms Days.</param>
public record CreateInvoiceCommand(
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    List<CreateInvoiceLineCommand> Lines,
    int PaymentTermsDays = 30
);

/// <summary>
/// Creates an invoice line command.
/// </summary>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="TaxRate">The tax Rate.</param>
/// <param name="Discount">The discount.</param>
public record CreateInvoiceLineCommand(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// Issue sue invoice command.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="IssueDate">The issue Date.</param>
public record IssueInvoiceCommand(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTime IssueDate
);

/// <summary>
/// Record payment command.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Amount">The amount.</param>
/// <param name="Method">The method.</param>
/// <param name="PaidAt">The paid At.</param>
/// <param name="ExternalPaymentId">The external Payment Id.</param>
public record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? ExternalPaymentId = null
);

/// <summary>
/// Cancel invoice command.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Reason">The reason.</param>
public record CancelInvoiceCommand(
    Guid InvoiceId,
    string Reason
);

/// <summary>
/// Creates a credit note command.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Lines">The lines.</param>
/// <param name="Reason">The reason.</param>
public record CreateCreditNoteCommand(
    Guid InvoiceId,
    List<CreditNoteLineData> Lines,
    string Reason
);
