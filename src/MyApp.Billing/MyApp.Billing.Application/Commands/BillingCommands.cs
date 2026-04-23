using MyApp.Billing.Domain.Entities;
using MyApp.Shared.CQRS;

namespace MyApp.Billing.Application.Commands;

/// <summary>
/// Command to create an invoice from order data
/// </summary>
public record CreateInvoiceCommand(
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    List<CreateInvoiceLineCommand> Lines,
    int PaymentTermsDays = 30
);

/// <summary>
/// Command to create an invoice line
/// </summary>
public record CreateInvoiceLineCommand(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// Command to issue an invoice
/// </summary>
public record IssueInvoiceCommand(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTime IssueDate
);

/// <summary>
/// Command to record a payment
/// </summary>
public record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? ExternalPaymentId = null
);

/// <summary>
/// Command to cancel an invoice
/// </summary>
public record CancelInvoiceCommand(
    Guid InvoiceId,
    string Reason
);

/// <summary>
/// Command to create a credit note
/// </summary>
public record CreateCreditNoteCommand(
    Guid InvoiceId,
    List<CreditNoteLineData> Lines,
    string Reason
);
