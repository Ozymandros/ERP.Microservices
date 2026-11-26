using MyApp.Projects.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Projects.Infrastructure.Adapters;

public sealed class OrdersCreationAdapter(IServiceInvoker serviceInvoker) : IOrdersCreationPort
{
    public async Task<ProjectOrderSnapshot> CreateWithReservationAsync(
        CreateProjectOrderWithReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var wire = new CreateOrderWithReservationWire
        {
            OrderNumber = request.OrderNumber,
            Type = request.OrderType,
            SourceId = request.SourceId,
            TargetId = request.TargetId,
            ExternalOrderId = request.ExternalOrderId,
            ExternalOrderSource = request.ExternalOrderSource,
            WarehouseId = request.WarehouseId,
            OrderDate = request.OrderDate,
            DestinationAddress = request.DestinationAddress,
            Lines = request.Lines.Select(l => new OrderLineWire { ProductId = l.ProductId, Quantity = l.Quantity }).ToList()
        };

        var result = await serviceInvoker.InvokeAsync<CreateOrderWithReservationWire, OrderWire>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.WithReservation,
            HttpMethod.Post,
            wire,
            cancellationToken);

        return new ProjectOrderSnapshot(result.Id, result.OrderNumber ?? string.Empty);
    }

    internal sealed class OrderWire
    {
        public Guid Id { get; init; }
        public string? OrderNumber { get; init; }
    }

    internal sealed class CreateOrderWithReservationWire
    {
        public string? OrderNumber { get; init; }
        public string? Type { get; init; }
        public Guid? SourceId { get; init; }
        public Guid? TargetId { get; init; }
        public Guid? ExternalOrderId { get; init; }
        public string? ExternalOrderSource { get; init; }
        public Guid WarehouseId { get; init; }
        public DateTime OrderDate { get; init; }
        public string? DestinationAddress { get; init; }
        public List<OrderLineWire>? Lines { get; init; }
    }

    internal sealed class OrderLineWire
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }
}