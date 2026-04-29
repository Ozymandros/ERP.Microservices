namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Event raised when a purchase order is created.
/// </summary>
public record PurchaseOrderCreatedEvent(
    Guid PurchaseOrderId,
    Guid SupplierId,
    string OrderNumber,
    decimal TotalAmount
);

/// <summary>
/// Event raised when a purchase order is approved.
/// </summary>
public record PurchaseOrderApprovedEvent(
    Guid PurchaseOrderId,
    Guid SupplierId,
    DateTime ApprovedDate
);

/// <summary>
/// Event raised when a purchase order is received.
/// </summary>
public record PurchaseOrderReceivedEvent(
    Guid PurchaseOrderId,
    Guid WarehouseId,
    DateTime ReceivedDate
);

/// <summary>
/// Event raised when a line item in a purchase order is received.
/// </summary>
public record PurchaseOrderLineReceivedEvent(
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    int ReceivedQuantity,
    Guid WarehouseId
);
