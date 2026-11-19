using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Application.UseCases.Invoices;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Billing.Application.Services;

/// <summary>Driving adapter facade: delegates invoice operations to use cases and handles reads.</summary>
public class InvoiceService(
    IInvoiceRepository invoiceRepository,
    CreateInvoiceUseCase createInvoice,
    IssueInvoiceUseCase issueInvoice,
    RecordPaymentUseCase recordPayment,
    CancelInvoiceUseCase cancelInvoice,
    CreateCreditNoteUseCase createCreditNote,
    CreateFromProjectRequestUseCase createFromProject,
    CreateAndIssueFromFulfillmentUseCase createFromFulfillment,
    CreateCreditNoteFromReturnFulfillmentUseCase createCreditNoteFromReturn) : IInvoiceService
{
    /// <inheritdoc />
    public Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default) =>
        createInvoice.ExecuteAsync(dto, cancellationToken);

    /// <inheritdoc />
    public Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string invoiceNumber, DateTime issueDate, CancellationToken cancellationToken = default) =>
        issueInvoice.ExecuteAsync(invoiceId, invoiceNumber, issueDate, cancellationToken);

    /// <inheritdoc />
    public Task<InvoiceDto> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default) =>
        recordPayment.ExecuteAsync(dto, cancellationToken);

    /// <inheritdoc />
    public Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default) =>
        cancelInvoice.ExecuteAsync(invoiceId, reason, cancellationToken);

    /// <inheritdoc />
    public Task<CreditNoteDto> CreateCreditNoteAsync(CreateCreditNoteDto dto, CancellationToken cancellationToken = default) =>
        createCreditNote.ExecuteAsync(dto, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        return invoice != null ? BillingInvoiceMapper.ToDto(invoice) : null;
    }

    /// <inheritdoc />
    public async Task<InvoiceDto?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        return invoice != null ? BillingInvoiceMapper.ToDto(invoice) : null;
    }

    /// <inheritdoc />
    public async Task<List<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var invoices = await invoiceRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return invoices.Select(BillingInvoiceMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<InvoiceDto>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await invoiceRepository.GetOpenInvoicesAsync(cancellationToken);
        return invoices.Select(BillingInvoiceMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var invoices = await invoiceRepository.GetInvoicesByOrderIdAsync(orderId, cancellationToken);
        return invoices.Select(BillingInvoiceMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<InvoiceDto>> QueryInvoicesAsync(ISpecification<Invoice> spec, CancellationToken cancellationToken = default)
    {
        var result = await invoiceRepository.QueryAsync(spec);
        var invoices = result.Items.Select(BillingInvoiceMapper.ToDto).ToList();
        return new PaginatedResult<InvoiceDto>(invoices, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <inheritdoc />
    public Task<InvoiceDto?> CreateFromProjectRequestAsync(
        ProjectInvoiceRequestedEvent @event,
        bool autoIssue,
        CancellationToken cancellationToken = default) =>
        createFromProject.ExecuteAsync(@event, autoIssue, cancellationToken);

    /// <inheritdoc />
    public Task<InvoiceDto?> CreateAndIssueFromFulfillmentAsync(
        OrderFulfilledEvent @event,
        CancellationToken cancellationToken = default) =>
        createFromFulfillment.ExecuteAsync(@event, cancellationToken);

    /// <inheritdoc />
    public Task<CreditNoteDto?> CreateCreditNoteFromReturnFulfillmentAsync(
        OrderFulfilledEvent @event,
        CancellationToken cancellationToken = default) =>
        createCreditNoteFromReturn.ExecuteAsync(@event, cancellationToken);
}