namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Purchase order created event.
/// </summary>
/// <param name="PurchaseOrderId">The purchase Order Id.</param>
/// <param name="SupplierId">The supplier Id.</param>
/// <param name="OrderNumber">The order Number.</param>
/// <param name="TotalAmount">The total Amount.</param>
public record PurchaseOrderCreatedEvent(
    Guid PurchaseOrderId,
    Guid SupplierId,
    string OrderNumber,
    decimal TotalAmount
);

/// <summary>
/// Purchase order approved event.
/// </summary>
/// <param name="PurchaseOrderId">The purchase Order Id.</param>
/// <param name="SupplierId">The supplier Id.</param>
/// <param name="ApprovedDate">The approved Date.</param>
public record PurchaseOrderApprovedEvent(
    Guid PurchaseOrderId,
    Guid SupplierId,
    DateTime ApprovedDate
);

/// <summary>
/// Purchase order received event.
/// </summary>
/// <param name="PurchaseOrderId">The purchase Order Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="ReceivedDate">The received Date.</param>
public record PurchaseOrderReceivedEvent(
    Guid PurchaseOrderId,
    Guid WarehouseId,
    DateTime ReceivedDate
);

/// <summary>
/// Purchase order line received event.
/// </summary>
/// <param name="PurchaseOrderId">The purchase Order Id.</param>
/// <param name="PurchaseOrderLineId">The purchase Order Line Id.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="ReceivedQuantity">The received Quantity.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
public record PurchaseOrderLineReceivedEvent(
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    int ReceivedQuantity,
    Guid WarehouseId
);
