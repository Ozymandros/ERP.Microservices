namespace MyApp.Sales.Application.Contracts.Ports;

/// <summary>Creates operational fulfillment orders via the Orders service.</summary>
public interface IOrdersFulfillmentPort
{
    Task<OperationalOrderSnapshot> CreateWithReservationAsync(
        CreateFulfillmentOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalOrderSnapshot> CreateOrderAsync(
        CreateOperationalOrderRequest request,
        CancellationToken cancellationToken = default);
}