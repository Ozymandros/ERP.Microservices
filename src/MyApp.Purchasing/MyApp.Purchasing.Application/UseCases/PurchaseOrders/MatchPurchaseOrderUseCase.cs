using Microsoft.Extensions.Logging;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Ports;
using MyApp.Purchasing.Application.Internals;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Purchasing.Application.UseCases.PurchaseOrders;

public sealed class MatchPurchaseOrderUseCase(
    IPurchaseOrderRepository purchaseOrderRepository,
    ISupplierInvoiceQueryPort supplierInvoices,
    IPurchasingTransactionBoundary boundary,
    ILogger<MatchPurchaseOrderUseCase> logger)
{
    public async Task<PurchaseOrderMatchResultDto> ExecuteAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var order = await purchaseOrderRepository.GetWithLinesAsync(purchaseOrderId)
            ?? throw new InvalidOperationException($"Purchase order {purchaseOrderId} not found");

        var supplierInvoiceList = await supplierInvoices.GetByPurchaseOrderIdAsync(purchaseOrderId, cancellationToken);

        if (supplierInvoiceList.Count == 0)
        {
            order.MatchStatus = PurchaseOrderMatchStatus.Unmatched;
            order.MatchedAt = DateTime.UtcNow;
            await purchaseOrderRepository.UpdateAsync(order);
            await boundary.SaveChangesAsync(cancellationToken);
            return new PurchaseOrderMatchResultDto(
                purchaseOrderId,
                order.MatchStatus,
                order.MatchedAt.Value,
                "No supplier invoices found for purchase order.");
        }

        var billedByProduct = supplierInvoiceList
            .SelectMany(i => i.Lines)
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var hasOverbill = false;
        var hasPartial = false;
        var allMatched = true;

        foreach (var line in order.Lines)
        {
            var hasBilledLine = billedByProduct.TryGetValue(line.ProductId, out var billedQty);
            if (!hasBilledLine && (line.Quantity > 0 || line.ReceivedQuantity > 0))
            {
                logger.LogWarning(
                    "PO line product {ProductId} missing from supplier invoices for PO {PurchaseOrderId}",
                    line.ProductId,
                    purchaseOrderId);
                hasPartial = true;
                allMatched = false;
                continue;
            }

            if (billedQty > line.ReceivedQuantity)
                hasOverbill = true;
            if (billedQty != line.Quantity || line.ReceivedQuantity != line.Quantity || billedQty != line.ReceivedQuantity)
            {
                hasPartial = true;
                allMatched = false;
            }
        }

        order.MatchStatus = hasOverbill
            ? PurchaseOrderMatchStatus.Overbilled
            : allMatched && !hasPartial
                ? PurchaseOrderMatchStatus.Matched
                : PurchaseOrderMatchStatus.Partial;

        order.MatchedAt = DateTime.UtcNow;
        await purchaseOrderRepository.UpdateAsync(order);
        await boundary.SaveChangesAsync(cancellationToken);

        if (order.MatchStatus == PurchaseOrderMatchStatus.Matched)
        {
            await boundary.EventPublisher.PublishAsync(
                MessagingConstants.Topics.PurchasingOrderMatched,
                new PurchaseOrderMatchedEvent(order.Id, order.MatchStatus.ToString(), order.MatchedAt.Value),
                cancellationToken);
        }

        var summary = $"MatchStatus={order.MatchStatus}; lines={order.Lines.Count}; supplierInvoices={supplierInvoiceList.Count}";
        logger.LogInformation("Purchase order {PurchaseOrderId} match completed: {Summary}", purchaseOrderId, summary);
        return new PurchaseOrderMatchResultDto(purchaseOrderId, order.MatchStatus, order.MatchedAt.Value, summary);
    }
}
