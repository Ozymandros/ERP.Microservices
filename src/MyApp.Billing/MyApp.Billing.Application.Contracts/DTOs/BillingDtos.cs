namespace MyApp.Billing.Application.Contracts.DTOs;

/// <summary>
/// Creates an invoice dto.
/// </summary>
/// <param name="InvoiceNumber">The invoice number.</param>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="OrderId">The related order identifier, if any.</param>
/// <param name="Currency">The currency code.</param>
/// <param name="Lines">The invoice line items.</param>
/// <param name="PaymentTermsDays">The payment terms in days.</param>
public record CreateInvoiceDto(
    string InvoiceNumber, // Now required, not nullable
    Guid CustomerId,
    Guid? OrderId,
    string Currency,
    List<CreateInvoiceLineDto> Lines,
    int PaymentTermsDays = 30
);

/// <summary>
/// Creates an invoice line dto.
/// </summary>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="TaxRate">The tax Rate.</param>
/// <param name="Discount">The discount.</param>
public record CreateInvoiceLineDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// Issue sue invoice dto.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="IssueDate">The issue Date.</param>
public record IssueInvoiceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTime IssueDate
);

/// <summary>
/// Record payment dto.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Amount">The amount.</param>
/// <param name="Method">The method.</param>
/// <param name="PaidAt">The paid At.</param>
/// <param name="ExternalPaymentId">The external Payment Id.</param>
public record RecordPaymentDto(
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? ExternalPaymentId = null
);

/// <summary>
/// Creates a credit note dto.
/// </summary>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Lines">The lines.</param>
/// <param name="Reason">The reason.</param>
public record CreateCreditNoteDto(
    Guid InvoiceId,
    List<CreditNoteLineDto> Lines,
    string Reason
);

/// <summary>
/// Credit note line dto.
/// </summary>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="TaxRate">The tax Rate.</param>
/// <param name="Discount">The discount.</param>
public record CreditNoteLineDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0
);

/// <summary>
/// Invoice dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderId">The order Id.</param>
/// <param name="Currency">The currency.</param>
/// <param name="Status">The status.</param>
/// <param name="IssueDate">The issue Date.</param>
/// <param name="DueDate">The due Date.</param>
/// <param name="TotalNet">The total Net.</param>
/// <param name="TotalTax">The total Tax.</param>
/// <param name="TotalGross">The total Gross.</param>
/// <param name="OutstandingAmount">The outstanding Amount.</param>
/// <param name="Lines">The lines.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
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
/// Invoice line dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="Discount">The discount.</param>
/// <param name="TaxRate">The tax Rate.</param>
/// <param name="LineNet">The line Net.</param>
/// <param name="LineTax">The line Tax.</param>
/// <param name="LineGross">The line Gross.</param>
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
/// Payment dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="InvoiceId">The invoice Id.</param>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">The currency.</param>
/// <param name="Method">The method.</param>
/// <param name="Status">The status.</param>
/// <param name="PaidAt">The paid At.</param>
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
/// Credit note dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="OriginalInvoiceId">The original Invoice Id.</param>
/// <param name="Reason">The reason.</param>
/// <param name="Status">The status.</param>
/// <param name="TotalNet">The total Net.</param>
/// <param name="TotalTax">The total Tax.</param>
/// <param name="TotalGross">The total Gross.</param>
/// <param name="CreatedAt">The created At.</param>
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
/// Issue sue invoice request.
/// </summary>
/// <param name="InvoiceNumber">The invoice Number.</param>
/// <param name="IssueDate">The issue Date.</param>
public record IssueInvoiceRequest(string InvoiceNumber, DateTime IssueDate);

/// <summary>
/// Cancel invoice request.
/// </summary>
/// <param name="Reason">The reason.</param>
public record CancelInvoiceRequest(string Reason);
