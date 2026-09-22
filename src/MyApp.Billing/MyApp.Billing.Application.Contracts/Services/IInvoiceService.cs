using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Billing.Application.Contracts.Services;

/// <summary>
/// Service contract for invoice operations
/// </summary>
public interface IInvoiceService
{
    /// <summary>Creates a new draft invoice from the supplied data.</summary>
    /// <param name="dto">The data required to create the invoice.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The created invoice as a <see cref="InvoiceDto"/>.</returns>
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default);

    /// <summary>Issues a draft invoice, assigning it a number and locking its lines.</summary>
    /// <param name="invoiceId">The identifier of the invoice to issue.</param>
    /// <param name="invoiceNumber">The unique invoice number to assign.</param>
    /// <param name="issueDate">The date the invoice is officially issued.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated invoice as a <see cref="InvoiceDto"/>.</returns>
    Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string invoiceNumber, DateTime issueDate, CancellationToken cancellationToken = default);

    /// <summary>Records a payment against an existing invoice.</summary>
    /// <param name="dto">The payment data to record.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated invoice as a <see cref="InvoiceDto"/>.</returns>
    Task<InvoiceDto> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default);

    /// <summary>Cancels an invoice that has not yet been fully paid.</summary>
    /// <param name="invoiceId">The identifier of the invoice to cancel.</param>
    /// <param name="reason">A human-readable reason for the cancellation.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The cancelled invoice as a <see cref="InvoiceDto"/>.</returns>
    Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Creates and issues a credit note against an existing invoice.</summary>
    /// <param name="dto">The data required to create the credit note.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The created credit note as a <see cref="CreditNoteDto"/>.</returns>
    Task<CreditNoteDto> CreateCreditNoteAsync(CreateCreditNoteDto dto, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single invoice by its unique identifier.</summary>
    /// <param name="invoiceId">The identifier of the invoice to retrieve.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The <see cref="InvoiceDto"/>, or <see langword="null"/> if not found.</returns>
    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single invoice by its human-readable invoice number.</summary>
    /// <param name="invoiceNumber">The invoice number to search for.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The <see cref="InvoiceDto"/>, or <see langword="null"/> if not found.</returns>
    Task<InvoiceDto?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all invoices belonging to a specific customer.</summary>
    /// <param name="customerId">The identifier of the customer.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of invoices for the specified customer.</returns>
    Task<List<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all invoices with an open (unpaid, non-cancelled) status.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of open invoices.</returns>
    Task<List<InvoiceDto>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves all invoices linked to a specific order.</summary>
    /// <param name="orderId">The identifier of the order.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of invoices associated with the specified order.</returns>
    Task<List<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Executes a paginated query against all invoices using the supplied specification.</summary>
    /// <param name="spec">The specification containing filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A paginated result set of <see cref="InvoiceDto"/> items.</returns>
    Task<PaginatedResult<InvoiceDto>> QueryInvoicesAsync(ISpecification<Invoice> spec, CancellationToken cancellationToken = default);
}
