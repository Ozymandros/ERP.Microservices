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
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default);
    Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string invoiceNumber, DateTime issueDate, CancellationToken cancellationToken = default);
    Task<InvoiceDto> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default);
    Task<CreditNoteDto> CreateCreditNoteAsync(CreateCreditNoteDto dto, CancellationToken cancellationToken = default);

    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<List<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<InvoiceDto>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default);
    Task<List<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<InvoiceDto>> QueryInvoicesAsync(ISpecification<Invoice> spec, CancellationToken cancellationToken = default);
}
