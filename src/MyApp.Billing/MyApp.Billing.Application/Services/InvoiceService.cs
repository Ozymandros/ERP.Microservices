using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
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
    private readonly IServiceInvoker _serviceInvoker;
    private readonly ILogger<InvoiceService> _logger;

    /// <summary>
    /// Initializes a new instance of the InvoiceService class.
    /// </summary>
    /// <param name="invoiceRepository">The invoice Repository.</param>
    /// <param name="creditNoteRepository">The credit Note Repository.</param>
    /// <param name="serviceInvoker">The Dapr service invoker for cross-service calls.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ICreditNoteRepository creditNoteRepository,
        IServiceInvoker serviceInvoker,
        ILogger<InvoiceService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Billing)
    {
        _invoiceRepository = invoiceRepository;
        _creditNoteRepository = creditNoteRepository;
        _serviceInvoker = serviceInvoker;
        _logger = logger;
    }

    /// <summary>
    /// Creates an invoice asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
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

        if (dto.OrderId.HasValue)
            invoice.LinkToSalesOrder(dto.OrderId.Value);

        if (!string.IsNullOrWhiteSpace(dto.SourceType) && dto.SourceId.HasValue)
            invoice.LinkToSource(dto.SourceType, dto.SourceId.Value);

        await _invoiceRepository.AddAsync(invoice);
        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync(MessagingConstants.Topics.BillingInvoiceCreated, new InvoiceCreatedEvent(
            invoice.Id,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.Currency,
            invoice.TotalGross,
            invoice.SourceType,
            invoice.SourceId
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Issue sue invoice asynchronously.
    /// </summary>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="issueDate">The issue Date.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
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
        await EventPublisher.PublishAsync(MessagingConstants.Topics.BillingInvoiceIssued, new InvoiceIssuedEvent(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.TotalNet,
            invoice.TotalTax,
            invoice.TotalGross,
            invoice.DueDate!.Value,
            invoice.SourceType,
            invoice.SourceId,
            invoice.Currency
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Record payment asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<InvoiceDto> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId)
            ?? throw new NotFoundException($"Invoice {dto.InvoiceId} not found");

        invoice.RecordPayment(dto.Amount, dto.Method, dto.PaidAt, dto.ExternalPaymentId);

        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync(MessagingConstants.Topics.BillingInvoicePaid, new InvoicePaidEvent(
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
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        invoice.Cancel();

        await _invoiceRepository.UpdateAsync(invoice);
        await SaveChangesAsync(cancellationToken);

        // Publish domain event
        await EventPublisher.PublishAsync(MessagingConstants.Topics.BillingInvoiceCancelled, new InvoiceCancelledEvent(
            invoice.Id,
            invoice.InvoiceNumber,
            reason,
            invoice.SourceType,
            invoice.SourceId
        ), cancellationToken);

        return MapToDto(invoice);
    }

    /// <summary>
    /// Creates a credit note asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
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
        await EventPublisher.PublishAsync(MessagingConstants.Topics.BillingCreditNoteIssued, new CreditNoteIssuedEvent(
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
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        return invoice != null ? MapToDto(invoice) : null;
    }

    /// <summary>
    /// Gets the invoice by invoice number asynchronously.
    /// </summary>
    /// <param name="invoiceNumber">The invoice Number.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<InvoiceDto?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        return invoice != null ? MapToDto(invoice) : null;
    }

    /// <summary>
    /// Gets the invoices by customer id asynchronously.
    /// </summary>
    /// <param name="customerId">The customer Id.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<List<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets the open invoices asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<List<InvoiceDto>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetOpenInvoicesAsync(cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets the invoices by order id asynchronously.
    /// </summary>
    /// <param name="orderId">The order Id.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<List<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetInvoicesByOrderIdAsync(orderId, cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Query invoices asynchronously.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<PaginatedResult<InvoiceDto>> QueryInvoicesAsync(ISpecification<Invoice> spec, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceRepository.QueryAsync(spec);
        var invoices = result.Items.Select(MapToDto).ToList();
        return new PaginatedResult<InvoiceDto>(invoices, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <inheritdoc />
    public async Task<InvoiceDto?> CreateFromProjectRequestAsync(
        ProjectInvoiceRequestedEvent @event,
        bool autoIssue,
        CancellationToken cancellationToken = default)
    {
        if (@event.Lines.Count == 0)
        {
            _logger.LogWarning("Project invoice request {RequestId} has no lines; skipping", @event.RequestId);
            return null;
        }

        // Idempotency: pub/sub is at-least-once, so a redelivered request must not create a second invoice.
        var existing = await _invoiceRepository.GetBySourceAsync(DocumentSources.Project, @event.RequestId, cancellationToken);
        var current = existing.FirstOrDefault(i => i.Status != InvoiceStatus.Cancelled);
        if (current is not null)
        {
            _logger.LogInformation(
                "Invoice {InvoiceId} already exists for project invoice request {RequestId}",
                current.Id, @event.RequestId);
            return MapToDto(current);
        }

        var draftNumber = $"DRAFT-{@event.RequestId:N}"[..20];
        var createDto = new CreateInvoiceDto(
            draftNumber,
            @event.CustomerId,
            null,
            string.IsNullOrWhiteSpace(@event.Currency) ? "USD" : @event.Currency.ToUpperInvariant(),
            @event.Lines.Select(l => new CreateInvoiceLineDto(
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.TaxRate,
                l.Discount)).ToList(),
            SourceType: DocumentSources.Project,
            SourceId: @event.RequestId);

        var created = await CreateInvoiceAsync(createDto, cancellationToken);

        _logger.LogInformation(
            "Created {Status} invoice {InvoiceId} for project {ProjectNumber} (request {RequestId})",
            autoIssue ? "auto-issued" : "draft", created.Id, @event.ProjectNumber, @event.RequestId);

        if (!autoIssue)
            return created;

        var issueNumber = $"INV-{@event.ProjectNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (issueNumber.Length > 50)
            issueNumber = issueNumber[..50];
        return await IssueInvoiceAsync(created.Id, issueNumber, DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<InvoiceDto?> CreateAndIssueFromFulfillmentAsync(
        OrderFulfilledEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(@event.OrderType, OrderTypes.Outbound, StringComparison.OrdinalIgnoreCase)
            || !@event.ExternalOrderId.HasValue)
        {
            return null;
        }

        // Only sales-originated movements are invoiced here. Project consumption orders are
        // invoiced through projects.invoice.requested (see CreateFromProjectRequestAsync).
        if (!DocumentSources.IsSales(@event.ExternalOrderSource))
        {
            _logger.LogInformation(
                "Skipping auto-invoice for order {OrderId}: external source {Source} is not Sales",
                @event.OrderId, @event.ExternalOrderSource);
            return null;
        }

        var salesOrderId = @event.ExternalOrderId.Value;
        var existing = await _invoiceRepository.GetInvoicesByOrderIdAsync(salesOrderId, cancellationToken);
        if (existing.Any(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.Paid or InvoiceStatus.Sent))
        {
            _logger.LogInformation("Invoice already exists for sales order {SalesOrderId}", salesOrderId);
            return existing.Select(MapToDto).First();
        }

        var salesOrder = await _serviceInvoker.InvokeAsync<SalesOrderDto>(
            ServiceNames.Sales,
            $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
            HttpMethod.Get,
            cancellationToken);

        if (salesOrder.Lines.Count == 0)
        {
            _logger.LogWarning("Sales order {SalesOrderId} has no lines; skipping invoice", salesOrderId);
            return null;
        }

        var draftNumber = $"DRAFT-{salesOrderId:N}"[..20];
        var createDto = new CreateInvoiceDto(
            draftNumber,
            salesOrder.CustomerId,
            salesOrderId,
            "USD",
            salesOrder.Lines.Select(l => new CreateInvoiceLineDto(
                $"Product {l.ProductId}",
                l.Quantity,
                l.UnitPrice,
                0m,
                0m)).ToList());

        var created = await CreateInvoiceAsync(createDto, cancellationToken);
        var issueNumber = $"INV-{salesOrder.OrderNumber}-{DateTime.UtcNow:yyyyMMdd}";
        return await IssueInvoiceAsync(created.Id, issueNumber, DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CreditNoteDto?> CreateCreditNoteFromReturnFulfillmentAsync(
        OrderFulfilledEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(@event.OrderType, OrderTypes.Return, StringComparison.OrdinalIgnoreCase)
            || !@event.ExternalOrderId.HasValue)
        {
            return null;
        }

        if (!DocumentSources.IsSales(@event.ExternalOrderSource))
        {
            _logger.LogInformation(
                "Skipping credit note for return order {OrderId}: external source {Source} is not Sales",
                @event.OrderId, @event.ExternalOrderSource);
            return null;
        }

        var salesOrderId = @event.ExternalOrderId.Value;
        var invoices = await _invoiceRepository.GetInvoicesByOrderIdAsync(salesOrderId, cancellationToken);
        var invoice = invoices.FirstOrDefault(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.Paid or InvoiceStatus.Sent);
        if (invoice is null)
        {
            _logger.LogWarning("No issued invoice for return on sales order {SalesOrderId}", salesOrderId);
            return null;
        }

        var salesOrder = await _serviceInvoker.InvokeAsync<SalesOrderDto>(
            ServiceNames.Sales,
            $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
            HttpMethod.Get,
            cancellationToken);

        var creditLines = new List<CreditNoteLineDto>();
        foreach (var eventLine in @event.Lines)
        {
            var salesLine = salesOrder.Lines.FirstOrDefault(l => l.ProductId == eventLine.ProductId);
            if (salesLine is null)
                continue;

            creditLines.Add(new CreditNoteLineDto(
                $"Return Product {eventLine.ProductId}",
                eventLine.Quantity,
                salesLine.UnitPrice,
                0m,
                0m));
        }

        if (creditLines.Count == 0)
            return null;

        return await CreateCreditNoteAsync(
            new CreateCreditNoteDto(invoice.Id, creditLines, "Sales return fulfilled"),
            cancellationToken);
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
            invoice.UpdatedAt ?? invoice.CreatedAt,
            invoice.SourceType,
            invoice.SourceId
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
