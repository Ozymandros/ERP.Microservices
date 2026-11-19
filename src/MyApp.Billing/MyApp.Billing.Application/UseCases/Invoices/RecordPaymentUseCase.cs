using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Records a payment against an invoice.</summary>
public sealed class RecordPaymentUseCase(
    IInvoiceRepository invoiceRepository,
    IBillingTransactionBoundary boundary)
{
    /// <inheritdoc />
    public async Task<InvoiceDto> ExecuteAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(dto.InvoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice {dto.InvoiceId} not found");

        invoice.RecordPayment(dto.Amount, dto.Method, dto.PaidAt, dto.ExternalPaymentId);

        await boundary.SaveChangesAsync(cancellationToken);

        await boundary.EventPublisher.PublishAsync(
            MessagingConstants.Topics.BillingInvoicePaid,
            new InvoicePaidEvent(
                invoice.Id,
                invoice.OrderId,
                invoice.CustomerId,
                dto.Amount,
                dto.PaidAt,
                dto.Method),
            cancellationToken);

        return BillingInvoiceMapper.ToDto(invoice);
    }
}