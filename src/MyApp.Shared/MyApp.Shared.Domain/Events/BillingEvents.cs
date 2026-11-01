namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Invoice created event.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The sales order Id when applicable.</param>
/// <param name="Currency">The currency.</param>
/// <param name="TotalGross">The total Gross.</param>
public record InvoiceCreatedEvent(
    Guid InvoiceId,
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    decimal TotalGross
);

/// <summary>Raised when a draft invoice is officially issued to a customer.</summary>
public record InvoiceIssuedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    Guid? OrderId,
    decimal TotalNet,
    decimal TotalTax,
    decimal TotalGross,
    DateTime DueDate
);

/// <summary>Raised when payment is recorded against an invoice.</summary>
public record InvoicePaidEvent(
    Guid InvoiceId,
    Guid? OrderId,
    Guid CustomerId,
    decimal PaidAmount,
    DateTime PaidAt,
    string PaymentMethod
);

/// <summary>Raised when an invoice is cancelled.</summary>
public record InvoiceCancelledEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string Reason
);

/// <summary>Raised when a payment is successfully recorded against an invoice.</summary>
public record PaymentRecordedEvent(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt
);

/// <summary>Raised when a credit note is issued against an existing invoice.</summary>
public record CreditNoteIssuedEvent(
    Guid CreditNoteId,
    Guid OriginalInvoiceId,
    string InvoiceNumber,
    decimal TotalGross,
    string Reason
);
