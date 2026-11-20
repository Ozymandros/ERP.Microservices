using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Ports;
using MyApp.Sales.Application.Internals;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Constants;

namespace MyApp.Sales.Application.UseCases.SalesOrders;

public sealed class CreateReturnUseCase(
    ISalesOrderRepository orderRepository,
    IOrdersFulfillmentPort ordersFulfillment,
    IMapper mapper,
    ISalesTransactionBoundary boundary,
    ILogger<CreateReturnUseCase> logger)
{
    public async Task<SalesOrderDto> ExecuteAsync(
        Guid salesOrderId,
        CreateSalesReturnDto dto,
        CancellationToken cancellationToken = default)
    {
        var salesOrder = await orderRepository.GetByIdAsync(salesOrderId)
            ?? throw new InvalidOperationException($"Sales order {salesOrderId} not found");

        if (salesOrder.Status is not (SalesOrderStatus.Invoiced or SalesOrderStatus.Shipped))
            throw new InvalidOperationException($"Returns require Invoiced or Shipped status; current: {salesOrder.Status}");

        if (salesOrder.ReturnOrderId.HasValue)
            throw new InvalidOperationException($"Return already initiated for sales order {salesOrderId}");

        foreach (var returnLine in dto.Lines)
        {
            var original = salesOrder.Lines.FirstOrDefault(l => l.ProductId == returnLine.ProductId)
                ?? throw new InvalidOperationException($"Product {returnLine.ProductId} not on sales order");
            if (returnLine.Quantity > original.Quantity)
                throw new InvalidOperationException($"Return quantity exceeds original for product {returnLine.ProductId}");
        }

        var returnOrder = await ordersFulfillment.CreateOrderAsync(
            new CreateOperationalOrderRequest(
                $"{salesOrder.OrderNumber}-RET",
                OrderTypes.Return,
                null,
                dto.WarehouseId,
                salesOrderId,
                DocumentSources.Sales,
                dto.WarehouseId,
                DateTime.UtcNow,
                dto.Lines.Select(l => new FulfillmentOrderLineRequest(l.ProductId, l.Quantity)).ToList()),
            cancellationToken);

        salesOrder.ReturnOrderId = returnOrder.Id;
        await orderRepository.UpdateAsync(salesOrder);
        await boundary.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Return order {ReturnOrderId} created for sales order {SalesOrderId}", returnOrder.Id, salesOrderId);
        return mapper.Map<SalesOrderDto>(salesOrder);
    }
}