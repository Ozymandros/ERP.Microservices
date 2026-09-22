using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Events;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Billing.Application.Services;

/// <summary>
/// Service for managing invoices, including creation, issuance, payment recording, and credit note operations.
/// </summary>
public class InvoiceService : AppServiceBase, IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ILogger<InvoiceService> _logger;

    /// <summary>
    /// Initializes a new instance of the InvoiceService class.
    /// </summary>
    /// <param name="invoiceRepository">The invoice Repository.</param>
    /// <param name="creditNoteRepository">The credit Note Repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ICreditNoteRepository creditNoteRepository,
        ILogger<InvoiceService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Billing)
    {
        _invoiceRepository = invoiceRepository;
        _creditNoteRepository = creditNoteRepository;
        _logger = logger;    }

    /// <summary>
    /// Creates an invoice asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            throw new ArgumentException("InvoiceNumber must be a non-empty unique value.", nameof(dto.InvoiceNumber));

        var existingInvoice = await _invoiceRepository.GetByInvoiceNumberAsync(dto.InvoiceNumber, cancellationToken);
        if (existingInvoice is not null)
            throw new InvalidOperationException($"Invoice number '{dto.InvoiceNumber}' already exists.");

        var invoice = new Invoice(Guid.NewGuid(), dto.InvoiceNumber, dto.CustomerId, dto.Currency);

        foreach (var line in dto.Lines)
        {
            invoice.AddLine(line.Description, line.Quantity, line.UnitPrice, line.TaxRate, line.Discount);
        }

        await _invoiceRepository.AddAsync(invoice);
        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync("billing.invoice.created", new InvoiceCreatedEvent(
            invoice.Id,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.Currency,
            invoice.TotalGross
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Issue sue invoice asynchronously.
    /// </summary>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="issueDate">The issue Date.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string invoiceNumber, DateTime issueDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("InvoiceNumber must be a non-empty unique value.", nameof(invoiceNumber));

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        var existingInvoice = await _invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        if (existingInvoice is not null && existingInvoice.Id != invoiceId)
            throw new InvalidOperationException($"Invoice number '{invoiceNumber}' already exists.");

        invoice.Issue(invoiceNumber, issueDate, invoice.PaymentTermsDays);

        await _invoiceRepository.UpdateAsync(invoice);
        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync("billing.invoice.issued", new InvoiceIssuedEvent(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.TotalNet,
            invoice.TotalTax,
            invoice.TotalGross,
            invoice.DueDate!.Value
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Record payment asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<InvoiceDto> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId)
            ?? throw new NotFoundException($"Invoice {dto.InvoiceId} not found");

        invoice.RecordPayment(dto.Amount, dto.Method, dto.PaidAt, dto.ExternalPaymentId);

        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync("billing.invoice.paid", new InvoicePaidEvent(
            invoice.Id,
            invoice.OrderId,
            invoice.CustomerId,
            dto.Amount,
            dto.PaidAt,
            dto.Method
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Cancel invoice asynchronously.
    /// </summary>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        invoice.Cancel();

        await _invoiceRepository.UpdateAsync(invoice);
        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync("billing.invoice.cancelled", new InvoiceCancelledEvent(
            invoice.Id,
            invoice.InvoiceNumber,
            reason
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Creates a credit note asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<CreditNoteDto> CreateCreditNoteAsync(CreateCreditNoteDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId)
            ?? throw new NotFoundException($"Invoice {dto.InvoiceId} not found");

        var lines = dto.Lines.Select(l => new CreditNoteLineData(
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRate,
            l.Discount
        )).ToList();

        var creditNote = invoice.CreateCreditNote(lines, dto.Reason);

        await _creditNoteRepository.AddAsync(creditNote);
        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync("billing.creditnote.issued", new CreditNoteIssuedEvent(
            creditNote.Id,
            creditNote.OriginalInvoiceId,
            $"CN-{creditNote.Id.ToString()[..8]}",
            creditNote.TotalGross,
            creditNote.Reason
        ), cancellationToken);

        return MapToDto(creditNote);
    }

    /// <summary>
    /// Gets the invoice by id asynchronously.
    /// </summary>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        return invoice != null ? MapToDto(invoice) : null;
    }

    /// <summary>
    /// Gets the invoice by invoice number asynchronously.
    /// </summary>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<InvoiceDto?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        return invoice != null ? MapToDto(invoice) : null;
    }

    /// <summary>
    /// Gets the invoices by customer id asynchronously.
    /// </summary>
    /// <param name="customerId">The customer Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets the open invoices asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<InvoiceDto>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetOpenInvoicesAsync(cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets the invoices by order id asynchronously.
    /// </summary>
    /// <param name="orderId">The order Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetInvoicesByOrderIdAsync(orderId, cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Query invoices asynchronously.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<PaginatedResult<InvoiceDto>> QueryInvoicesAsync(ISpecification<Invoice> spec, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceRepository.QueryAsync(spec);
        var invoices = result.Items.Select(MapToDto).ToList();
        return new PaginatedResult<InvoiceDto>(invoices, result.PageNumber, result.PageSize, result.TotalCount);
    }

    private InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.Currency,
            invoice.Status.ToString(),
            invoice.IssueDate,
            invoice.DueDate,
            invoice.TotalNet,
            invoice.TotalTax,
            invoice.TotalGross,
            invoice.OutstandingAmount,
            invoice.Lines.Select(l => new InvoiceLineDto(
                l.Id,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.Discount,
                l.TaxRate,
                l.LineNet,
                l.LineTax,
                l.LineGross
            )).ToList(),
            invoice.CreatedAt,
            invoice.UpdatedAt ?? invoice.CreatedAt
        );
    }

    private CreditNoteDto MapToDto(CreditNote creditNote)
    {
        return new CreditNoteDto(
            creditNote.Id,
            creditNote.OriginalInvoiceId,
            creditNote.Reason,
            creditNote.Status.ToString(),
            creditNote.TotalNet,
            creditNote.TotalTax,
            creditNote.TotalGross,
            creditNote.CreatedAt
        );
    }
}
