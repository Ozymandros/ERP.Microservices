namespace MyApp.Projects.Application.Contracts.Ports;

public sealed record SalesCustomerSnapshot(Guid Id, string Name);

public sealed record InventoryProductSnapshot(
    Guid Id,
    string Name,
    string Sku,
    decimal UnitPrice);

public sealed record ProjectOrderSnapshot(Guid Id, string OrderNumber);

public sealed record CreateProjectOrderWithReservationRequest(
    string OrderNumber,
    string OrderType,
    Guid? SourceId,
    Guid? TargetId,
    Guid ExternalOrderId,
    string ExternalOrderSource,
    Guid WarehouseId,
    DateTime OrderDate,
    string? DestinationAddress,
    IReadOnlyList<ProjectOrderLineRequest> Lines);

public sealed record ProjectOrderLineRequest(Guid ProductId, int Quantity);