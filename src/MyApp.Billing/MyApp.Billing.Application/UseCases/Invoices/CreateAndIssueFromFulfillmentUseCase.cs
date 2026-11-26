using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Ports;
using MyApp.Billing.Application.Mapping;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Billing.Application.UseCases.Invoices;

/// <summary>Creates and issues an invoice when a sales outbound order is fulfilled.</summary>
public sealed class CreateAndIssueFromFulfillmentUseCase(
    IInvoiceRepository invoiceRepository,
    ISalesOrderLookupPort salesOrderLookup,
    CreateInvoiceUseCase createInvoice,
    IssueInvoiceUseCase issueInvoice,
    ILogger<CreateAndIssueFromFulfillmentUseCase> logger)
{
    /// <inheritdoc />
    public async Task<InvoiceDto?> ExecuteAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(@event.OrderType, OrderTypes.Outbound, StringComparison.OrdinalIgnoreCase)
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
                "Skipping auto-invoice for order {OrderId}: external source {Source} is not Sales",
                @event.OrderId, sanitizedExternalOrderSource);
            return null;
        }

        var salesOrderId = @event.ExternalOrderId.Value;
        var existing = await invoiceRepository.GetInvoicesByOrderIdAsync(salesOrderId, cancellationToken);
        if (existing.Any(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.Paid or InvoiceStatus.Sent))
        {
            logger.LogInformation("Invoice already exists for sales order {SalesOrderId}", salesOrderId);
            return existing.Select(BillingInvoiceMapper.ToDto).First();
        }

        var salesOrder = await salesOrderLookup.GetByIdAsync(salesOrderId, cancellationToken);
        if (salesOrder is null || salesOrder.Lines.Count == 0)
        {
            logger.LogWarning("Sales order {SalesOrderId} has no lines; skipping invoice", salesOrderId);
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

        var created = await createInvoice.ExecuteAsync(createDto, cancellationToken);
        var issueNumber = $"INV-{salesOrder.OrderNumber}-{DateTime.UtcNow:yyyyMMdd}";
        return await issueInvoice.ExecuteAsync(created.Id, issueNumber, DateTime.UtcNow, cancellationToken);
    }
}