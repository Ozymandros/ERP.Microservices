namespace MyApp.Sales.Application.Contracts.Ports;

/// <summary>Minimal operational order returned from Orders service.</summary>
public sealed record OperationalOrderSnapshot(Guid Id, string OrderNumber);

/// <summary>Stock availability for a product (ACL).</summary>
public sealed record ProductAvailabilitySnapshot(
    Guid ProductId,
    int TotalAvailable,
    IReadOnlyList<WarehouseAvailabilitySnapshot> WarehouseStocks);

public sealed record WarehouseAvailabilitySnapshot(
    Guid WarehouseId,
    string WarehouseName,
    int AvailableQuantity);

/// <summary>Request to create an outbound order with reservation in Orders.</summary>
public sealed record CreateFulfillmentOrderRequest(
    string OrderNumber,
    string OrderType,
    Guid? SourceId,
    Guid? TargetId,
    Guid ExternalOrderId,
    string? ExternalOrderSource,
    Guid WarehouseId,
    DateTime OrderDate,
    string? DestinationAddress,
    IReadOnlyList<FulfillmentOrderLineRequest> Lines);

public sealed record FulfillmentOrderLineRequest(Guid ProductId, int Quantity);

/// <summary>Request to create a simple operational order (e.g. return).</summary>
public sealed record CreateOperationalOrderRequest(
    string OrderNumber,
    string OrderType,
    Guid? SourceId,
    Guid? TargetId,
    Guid ExternalOrderId,
    string? ExternalOrderSource,
    Guid WarehouseId,
    DateTime OrderDate,
    IReadOnlyList<FulfillmentOrderLineRequest> Lines);