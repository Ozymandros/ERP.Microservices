using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Issues a draft invoice.</summary>
public sealed class IssueInvoiceUseCase(
    IInvoiceRepository invoiceRepository,
    IBillingTransactionBoundary boundary)
{
    /// <summary>Issues the invoice with the given number and date.</summary>
    public async Task<InvoiceDto> ExecuteAsync(
        Guid invoiceId,
        string invoiceNumber,
        DateTime issueDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("InvoiceNumber must be a non-empty unique value.", nameof(invoiceNumber));

        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found");

        var existingInvoice = await invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        if (existingInvoice is not null && existingInvoice.Id != invoiceId)
            throw new InvalidOperationException($"Invoice number '{invoiceNumber}' already exists.");

        invoice.Issue(invoiceNumber, issueDate, invoice.PaymentTermsDays);

        await invoiceRepository.UpdateAsync(invoice);
        await boundary.SaveChangesAsync(cancellationToken);

        await boundary.EventPublisher.PublishAsync(
            MessagingConstants.Topics.BillingInvoiceIssued,
            new InvoiceIssuedEvent(
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
                invoice.Currency),
            cancellationToken);

        return BillingInvoiceMapper.ToDto(invoice);
    }
}