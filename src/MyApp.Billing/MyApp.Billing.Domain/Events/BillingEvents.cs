namespace MyApp.Billing.Domain.Events;

/// <summary>
/// Invoice created event.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The order Id.</param>
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
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The order Id.</param>
/// <param name="TotalNet">The total Net.</param>
/// <param name="TotalTax">The total Tax.</param>
/// <param name="TotalGross">The total Gross.</param>
/// <param name="DueDate">The due Date.</param>
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

/// <summary>Raised when all outstanding amounts on an invoice have been paid.</summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="OrderId">The order Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="PaidAmount">The paid Amount.</param>
/// <param name="PaidAt">The paid At.</param>
/// <param name="PaymentMethod">The payment Method.</param>
public record InvoicePaidEvent(
    Guid InvoiceId,
    Guid? OrderId,
    Guid CustomerId,
    decimal PaidAmount,
    DateTime PaidAt,
    string PaymentMethod
);

/// <summary>Raised when an invoice is cancelled and is no longer valid for payment.</summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="Reason">The reason.</param>
public record InvoiceCancelledEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string Reason
);

/// <summary>Raised when a payment is successfully recorded against an invoice.</summary>
/// <param name="PaymentId">The payment Id.</param>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Amount">The amount.</param>
/// <param name="Method">The method.</param>
/// <param name="PaidAt">The paid At.</param>
public record PaymentRecordedEvent(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt
);

/// <summary>Raised when a credit note is issued against an existing invoice.</summary>
/// <param name="CreditNoteId">The credit Note Id.</param>
/// <param name="OriginalInvoiceId">The original Invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="TotalGross">The total Gross.</param>
/// <param name="Reason">The reason.</param>
public record CreditNoteIssuedEvent(
    Guid CreditNoteId,
    Guid OriginalInvoiceId,
    string InvoiceNumber,
    decimal TotalGross,
    string Reason
);
