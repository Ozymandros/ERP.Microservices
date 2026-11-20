namespace MyApp.Purchasing.Application.Contracts.Ports;

public sealed record OperationalOrderSnapshot(Guid Id, string OrderNumber);

public sealed record CreateInboundOrderRequest(
    string OrderNumber,
    Guid? SourceId,
    Guid? TargetId,
    Guid ExternalOrderId,
    string? ExternalOrderSource,
    Guid WarehouseId,
    DateTime OrderDate,
    IReadOnlyList<InboundOrderLineRequest> Lines);

public sealed record InboundOrderLineRequest(Guid ProductId, int Quantity);

public sealed record FulfillInboundOrderRequest(
    Guid OrderId,
    Guid WarehouseId,
    string? TrackingNumber,
    string? ShippingAddress);

public sealed record SupplierInvoiceSnapshot(
    Guid Id,
    IReadOnlyList<SupplierInvoiceLineSnapshot> Lines);

public sealed record SupplierInvoiceLineSnapshot(Guid ProductId, int Quantity);