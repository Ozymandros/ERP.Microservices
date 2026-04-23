using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Events;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Billing.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IEventPublisher _eventPublisher;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ICreditNoteRepository creditNoteRepository,
        IMapper mapper,
        ILogger<InvoiceService> logger,
        IEventPublisher eventPublisher)
    {
        _invoiceRepository = invoiceRepository;
        _creditNoteRepository = creditNoteRepository;
        _mapper = mapper;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = new Invoice(Guid.NewGuid(), dto.CustomerId, dto.Currency);
        
        foreach (var line in dto.Lines)
        {
            invoice.AddLine(line.Description, line.Quantity, line.UnitPrice, line.TaxRate, line.Discount);
        }

        await _invoiceRepository.AddAsync(invoice);

        // Publish domain event
        await _eventPublisher.PublishAsync(new InvoiceCreatedEvent(
            invoice.Id,
            invoice.CustomerId,
            invoice.OrderId,
            invoice.Currency,
            invoice.TotalGross
        ), cancellationToken);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string invoiceNumber, DateTime issueDate, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        invoice.Issue(invoiceNumber, issueDate, invoice.PaymentTermsDays);
        
        await _invoiceRepository.UpdateAsync(invoice);

        // Publish domain event
        await _eventPublisher.PublishAsync(new InvoiceIssuedEvent(
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

    public async Task<InvoiceDto> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId)
            ?? throw new NotFoundException($"Invoice {dto.InvoiceId} not found");

        invoice.RecordPayment(dto.Amount, dto.Method, dto.PaidAt, dto.ExternalPaymentId);
        
        await _invoiceRepository.UpdateAsync(invoice);

        // Publish domain event
        await _eventPublisher.PublishAsync(new InvoicePaidEvent(
            invoice.Id,
            invoice.OrderId,
            invoice.CustomerId,
            dto.Amount,
            dto.PaidAt,
            dto.Method
        ), cancellationToken);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        invoice.Cancel();
        
        await _invoiceRepository.UpdateAsync(invoice);

        // Publish domain event
        await _eventPublisher.PublishAsync(new InvoiceCancelledEvent(
            invoice.Id,
            invoice.InvoiceNumber,
            reason
        ), cancellationToken);

        return MapToDto(invoice);
    }

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

        // Publish domain event
        await _eventPublisher.PublishAsync(new CreditNoteIssuedEvent(
            creditNote.Id,
            creditNote.OriginalInvoiceId,
            $"CN-{creditNote.Id.ToString()[..8]}",
            creditNote.TotalGross,
            creditNote.Reason
        ), cancellationToken);

        return MapToDto(creditNote);
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        return invoice != null ? MapToDto(invoice) : null;
    }

    public async Task<List<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    public async Task<List<InvoiceDto>> GetOpenInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetOpenInvoicesAsync(cancellationToken);
        return invoices.Select(MapToDto).ToList();
    }

    public async Task<List<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetInvoicesByOrderIdAsync(orderId, cancellationToken);
        return invoices.Select(MapToDto).ToList();
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
            invoice.UpdatedAt
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
