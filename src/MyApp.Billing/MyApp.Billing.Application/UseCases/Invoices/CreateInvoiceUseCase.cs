using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Creates a draft customer invoice.</summary>
public sealed class CreateInvoiceUseCase(
    IInvoiceRepository invoiceRepository,
    IBillingTransactionBoundary boundary)
{
    /// <summary>Creates a draft invoice from the supplied DTO.</summary>
    public async Task<InvoiceDto> ExecuteAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            throw new ArgumentException("InvoiceNumber must be a non-empty unique value.", nameof(dto));

        var existingInvoice = await invoiceRepository.GetByInvoiceNumberAsync(dto.InvoiceNumber, cancellationToken);
        if (existingInvoice is not null)
            throw new InvalidOperationException($"Invoice number '{dto.InvoiceNumber}' already exists.");

        var invoice = new Invoice(Guid.NewGuid(), dto.InvoiceNumber, dto.CustomerId, dto.Currency);

        foreach (var line in dto.Lines)
            invoice.AddLine(line.Description, line.Quantity, line.UnitPrice, line.TaxRate, line.Discount);

        if (dto.OrderId.HasValue)
            invoice.LinkToSalesOrder(dto.OrderId.Value);

        if (!string.IsNullOrWhiteSpace(dto.SourceType) && dto.SourceId.HasValue)
            invoice.LinkToSource(dto.SourceType, dto.SourceId.Value);

        await invoiceRepository.AddAsync(invoice);
        await boundary.SaveChangesAsync(cancellationToken);

        await boundary.EventPublisher.PublishAsync(
            MessagingConstants.Topics.BillingInvoiceCreated,
            new InvoiceCreatedEvent(
                invoice.Id,
                invoice.CustomerId,
                invoice.OrderId,
                invoice.Currency,
                invoice.TotalGross,
                invoice.SourceType,
                invoice.SourceId),
            cancellationToken);

        return BillingInvoiceMapper.ToDto(invoice);
    }
}