namespace MyApp.Billing.Domain.Events;

/// <summary>
/// Domain events for the Billing service
/// </summary>
public record InvoiceCreatedEvent(
    Guid InvoiceId,
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    decimal TotalGross
);

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

public record InvoicePaidEvent(
    Guid InvoiceId,
    Guid? OrderId,
    Guid CustomerId,
    decimal PaidAmount,
    DateTime PaidAt,
    string PaymentMethod
);

public record InvoiceCancelledEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string Reason
);

public record PaymentRecordedEvent(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt
);

public record CreditNoteIssuedEvent(
    Guid CreditNoteId,
    Guid OriginalInvoiceId,
    string InvoiceNumber,
    decimal TotalGross,
    string Reason
);
