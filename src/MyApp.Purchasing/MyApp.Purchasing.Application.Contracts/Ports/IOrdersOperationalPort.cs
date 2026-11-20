namespace MyApp.Purchasing.Application.Contracts.Ports;

public interface IOrdersOperationalPort
{
    Task<OperationalOrderSnapshot> CreateInboundOrderAsync(
        CreateInboundOrderRequest request,
        CancellationToken cancellationToken = default);

    Task FulfillInboundOrderAsync(
        FulfillInboundOrderRequest request,
        CancellationToken cancellationToken = default);
}