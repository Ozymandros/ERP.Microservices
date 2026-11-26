using MyApp.Purchasing.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Purchasing.Infrastructure.Adapters;

public sealed class OrdersOperationalAdapter(IServiceInvoker serviceInvoker) : IOrdersOperationalPort
{
    public async Task<OperationalOrderSnapshot> CreateInboundOrderAsync(
        CreateInboundOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var wire = new CreateOrderWire
        {
            OrderNumber = request.OrderNumber,
            Type = OrderTypes.Inbound,
            SourceId = request.SourceId,
            TargetId = request.TargetId,
            ExternalOrderId = request.ExternalOrderId,
            ExternalOrderSource = request.ExternalOrderSource,
            WarehouseId = request.WarehouseId,
            OrderDate = request.OrderDate,
            Lines = request.Lines.Select(l => new OrderLineWire { ProductId = l.ProductId, Quantity = l.Quantity }).ToList()
        };

        var result = await serviceInvoker.InvokeAsync<CreateOrderWire, OrderWire>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Post,
            wire,
            cancellationToken);

        return new OperationalOrderSnapshot(result.Id, result.OrderNumber ?? string.Empty);
    }

    public async Task FulfillInboundOrderAsync(FulfillInboundOrderRequest request, CancellationToken cancellationToken = default)
    {
        var wire = new FulfillOrderWire
        {
            OrderId = request.OrderId,
            WarehouseId = request.WarehouseId,
            ShippingAddress = request.ShippingAddress,
            TrackingNumber = request.TrackingNumber
        };

        await serviceInvoker.InvokeAsync<FulfillOrderWire, OrderWire>(
            ServiceNames.Orders,
            $"{ApiEndpoints.Orders.Base}/{request.OrderId}/fulfill",
            HttpMethod.Post,
            wire,
            cancellationToken);
    }

    internal sealed class OrderWire
    {
        public Guid Id { get; init; }
        public string? OrderNumber { get; init; }
    }

    internal sealed class CreateOrderWire
    {
        public string? OrderNumber { get; init; }
        public string? Type { get; init; }
        public Guid? SourceId { get; init; }
        public Guid? TargetId { get; init; }
        public Guid? ExternalOrderId { get; init; }
        public string? ExternalOrderSource { get; init; }
        public Guid WarehouseId { get; init; }
        public DateTime OrderDate { get; init; }
        public List<OrderLineWire>? Lines { get; init; }
    }

    internal sealed class OrderLineWire
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }

    internal sealed class FulfillOrderWire
    {
        public Guid OrderId { get; init; }
        public Guid WarehouseId { get; init; }
        public string? ShippingAddress { get; init; }
        public string? TrackingNumber { get; init; }
    }
}