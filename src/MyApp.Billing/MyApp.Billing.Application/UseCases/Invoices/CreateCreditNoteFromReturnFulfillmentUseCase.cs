using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Ports;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Creates a credit note when a sales return order is fulfilled.</summary>
public sealed class CreateCreditNoteFromReturnFulfillmentUseCase(
    IInvoiceRepository invoiceRepository,
    ISalesOrderLookupPort salesOrderLookup,
    CreateCreditNoteUseCase createCreditNote,
    ILogger<CreateCreditNoteFromReturnFulfillmentUseCase> logger)
{
    /// <inheritdoc />
    public async Task<CreditNoteDto?> ExecuteAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(@event.OrderType, OrderTypes.Return, StringComparison.OrdinalIgnoreCase)
            || !@event.ExternalOrderId.HasValue)
        {
            return null;
        }

        if (!DocumentSources.IsSales(@event.ExternalOrderSource))
        {
            var sanitizedExternalOrderSource = (@event.ExternalOrderSource ?? string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            logger.LogInformation(
                "Skipping credit note for return order {OrderId}: external source {Source} is not Sales",
                @event.OrderId, sanitizedExternalOrderSource);
            return null;
        }

        var salesOrderId = @event.ExternalOrderId.Value;
        var invoices = await invoiceRepository.GetInvoicesByOrderIdAsync(salesOrderId, cancellationToken);
        var invoice = invoices.FirstOrDefault(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.Paid or InvoiceStatus.Sent);
        if (invoice is null)
        {
            logger.LogWarning("No issued invoice for return on sales order {SalesOrderId}", salesOrderId);
            return null;
        }

        var salesOrder = await salesOrderLookup.GetByIdAsync(salesOrderId, cancellationToken);
        if (salesOrder is null)
            return null;

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

        return await createCreditNote.ExecuteAsync(
            new CreateCreditNoteDto(invoice.Id, creditLines, "Sales return fulfilled"),
            cancellationToken);
    }
}