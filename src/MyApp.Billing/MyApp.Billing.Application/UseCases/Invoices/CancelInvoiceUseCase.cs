using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Cancels an invoice.</summary>
public sealed class CancelInvoiceUseCase(
    IInvoiceRepository invoiceRepository,
    IBillingTransactionBoundary boundary)
{
    /// <summary>Cancels the invoice with the given reason.</summary>
    public async Task<InvoiceDto> ExecuteAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        invoice.Cancel();

        await invoiceRepository.UpdateAsync(invoice);
        await boundary.SaveChangesAsync(cancellationToken);

        await boundary.EventPublisher.PublishAsync(
            MessagingConstants.Topics.BillingInvoiceCancelled,
            new InvoiceCancelledEvent(
                invoice.Id,
                invoice.InvoiceNumber,
                reason,
                invoice.SourceType,
                invoice.SourceId),
            cancellationToken);

        return BillingInvoiceMapper.ToDto(invoice);
    }
}