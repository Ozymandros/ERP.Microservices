using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Ports;
using MyApp.Sales.Application.Internals;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Sales.Application.UseCases.SalesOrders;

public sealed class ConfirmQuoteUseCase(
    ISalesOrderRepository orderRepository,
    IOrdersFulfillmentPort ordersFulfillment,
    CheckStockAvailabilityUseCase checkStock,
    IMapper mapper,
    ISalesTransactionBoundary boundary,
    ILogger<ConfirmQuoteUseCase> logger)
{
    public async Task<SalesOrderDto> ExecuteAsync(ConfirmQuoteDto dto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Confirming quote: QuoteId={QuoteId}", dto.QuoteId);

        var quote = await orderRepository.GetByIdAsync(dto.QuoteId, cancellationToken)
            ?? throw new InvalidOperationException($"Quote {dto.QuoteId} not found");

        if (!quote.IsQuote)
            throw new InvalidOperationException($"Sales Order {dto.QuoteId} is not a quote");

        if (quote.Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException($"Quote cannot be confirmed. Current status: {quote.Status}");

        if (quote.QuoteExpiryDate.HasValue && DateTime.UtcNow > quote.QuoteExpiryDate.Value)
            throw new InvalidOperationException($"Quote has expired on {quote.QuoteExpiryDate.Value}");

        var stockChecks = await checkStock.ExecuteAsync(
            quote.Lines.Select(l => new CreateUpdateSalesOrderLineDto(l.ProductId, l.Quantity, l.UnitPrice)).ToList(),
            cancellationToken);

        var unavailableItems = stockChecks.Where(s => !s.IsAvailable).ToList();
        if (unavailableItems.Count > 0)
        {
            var itemsList = string.Join(", ", unavailableItems.Select(i =>
                $"Product {i.ProductId}: requested {i.RequestedQuantity}, available {i.AvailableQuantity}"));
            throw new InvalidOperationException($"Insufficient stock for items: {itemsList}");
        }

        try
        {
            var fulfillmentOrder = await ordersFulfillment.CreateWithReservationAsync(
                new CreateFulfillmentOrderRequest(
                    $"{quote.OrderNumber}-ORD",
                    OrderTypes.Outbound,
                    dto.WarehouseId,
                    null,
                    quote.Id,
                    DocumentSources.Sales,
                    dto.WarehouseId,
                    DateTime.UtcNow,
                    dto.ShippingAddress,
                    quote.Lines.Select(l => new FulfillmentOrderLineRequest(l.ProductId, l.Quantity)).ToList()),
                cancellationToken);

            quote.Status = SalesOrderStatus.Confirmed;
            quote.ConvertedToOrderId = fulfillmentOrder.Id;
            await orderRepository.UpdateAsync(quote);
            await boundary.SaveChangesAsync(cancellationToken);

            try
            {
                await boundary.EventPublisher.PublishAsync(
                    MessagingConstants.Topics.SalesOrderConfirmed,
                    new SalesOrderConfirmedEvent(quote.Id, quote.ConvertedToOrderId.Value, DateTime.UtcNow),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish SalesOrderConfirmedEvent for Quote {QuoteId}", quote.Id);
            }

            return mapper.Map<SalesOrderDto>(quote);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create fulfillment order for Quote {QuoteId}", dto.QuoteId);
            throw new InvalidOperationException($"Failed to create fulfillment order: {ex.Message}", ex);
        }
    }
}
