namespace MyApp.Billing.Application.Contracts.DTOs;

/// <summary>
/// DTO for creating an invoice from an order
/// </summary>
public record CreateInvoiceDto(
    string InvoiceNumber, // Now required, not nullable
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    List<CreateInvoiceLineDto> Lines,
    int PaymentTermsDays = 30
);

/// <summary>
/// DTO for creating an invoice line
/// </summary>
public record CreateInvoiceLineDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// DTO for issuing an invoice
/// </summary>
public record IssueInvoiceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTime IssueDate
);

/// <summary>
/// DTO for recording a payment
/// </summary>
public record RecordPaymentDto(
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? ExternalPaymentId = null
);

/// <summary>
/// DTO for creating a credit note
/// </summary>
public record CreateCreditNoteDto(
    Guid InvoiceId,
    List<CreditNoteLineDto> Lines,
    string Reason
);

/// <summary>
/// DTO for a credit note line
/// </summary>
public record CreditNoteLineDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// DTO for invoice details
/// </summary>
public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    string Status,
    DateTime? IssueDate,
    DateTime? DueDate,
    decimal TotalNet,
    decimal TotalTax,
    decimal TotalGross,
    decimal OutstandingAmount,
    List<InvoiceLineDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for invoice line details
/// </summary>
public record InvoiceLineDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal TaxRate,
    decimal LineNet,
    decimal LineTax,
    decimal LineGross
);

/// <summary>
/// DTO for payment details
/// </summary>
public record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    DateTime PaidAt
);

/// <summary>
/// DTO for credit note details
/// </summary>
public record CreditNoteDto(
    Guid Id,
    Guid OriginalInvoiceId,
    string Reason,
    string Status,
    decimal TotalNet,
    decimal TotalTax,
    decimal TotalGross,
    DateTime CreatedAt
);

/// <summary>
/// Request body for issuing an invoice (InvoiceId is supplied via the route).
/// </summary>
public record IssueInvoiceRequest(string InvoiceNumber, DateTime IssueDate);

/// <summary>
/// Request body for cancelling an invoice (InvoiceId is supplied via the route).
/// </summary>
public record CancelInvoiceRequest(string Reason);
