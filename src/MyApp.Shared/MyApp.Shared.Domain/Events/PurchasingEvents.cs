namespace MyApp.Shared.Domain.Events;

public record PurchaseOrderCreatedEvent(
    Guid PurchaseOrderId,
    Guid SupplierId,
    string OrderNumber,
    decimal TotalAmount
);

public record PurchaseOrderApprovedEvent(
    Guid PurchaseOrderId,
    Guid SupplierId,
    DateTime ApprovedDate
);

public record PurchaseOrderReceivedEvent(
    Guid PurchaseOrderId,
    Guid WarehouseId,
    DateTime ReceivedDate
);

public record PurchaseOrderLineReceivedEvent(
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    int ReceivedQuantity,
    Guid WarehouseId
);
