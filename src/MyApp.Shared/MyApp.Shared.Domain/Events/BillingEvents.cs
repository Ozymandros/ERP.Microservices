namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Invoice created event.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The sales order Id when applicable.</param>
/// <param name="Currency">The currency.</param>
/// <param name="TotalGross">The total Gross.</param>
/// <param name="SourceType">Originating document type (see <c>DocumentSources</c>); null = Sales.</param>
/// <param name="SourceId">Originating document id.</param>
public record InvoiceCreatedEvent(
    Guid InvoiceId,
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    decimal TotalGross,
    string? SourceType = null,
    Guid? SourceId = null
);

/// <summary>Raised when a draft invoice is officially issued to a customer.</summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The sales order Id when applicable.</param>
/// <param name="TotalNet">The total Net.</param>
/// <param name="TotalTax">The total Tax.</param>
/// <param name="TotalGross">The total Gross.</param>
/// <param name="DueDate">The due Date.</param>
/// <param name="SourceType">Originating document type (see <c>DocumentSources</c>); null = Sales.</param>
/// <param name="SourceId">Originating document id (e.g. project invoice request id).</param>
/// <param name="Currency">ISO 4217 currency of the invoice.</param>
public record InvoiceIssuedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    Guid? OrderId,
    decimal TotalNet,
    decimal TotalTax,
    decimal TotalGross,
    DateTime DueDate,
    string? SourceType = null,
    Guid? SourceId = null,
    string? Currency = null
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
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="Reason">The cancellation reason.</param>
/// <param name="SourceType">Originating document type (see <c>DocumentSources</c>); null = Sales.</param>
/// <param name="SourceId">Originating document id.</param>
public record InvoiceCancelledEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string Reason,
    string? SourceType = null,
    Guid? SourceId = null
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
