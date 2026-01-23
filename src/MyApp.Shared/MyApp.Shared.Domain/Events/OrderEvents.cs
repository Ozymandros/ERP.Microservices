namespace MyApp.Shared.Domain.Events;

public record OrderLineEvent(
    Guid ProductId,
    int Quantity
);

public record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    string OrderType,
    List<OrderLineEvent> Lines
);

public record OrderFulfilledEvent(
    Guid OrderId,
    Guid WarehouseId,
    DateTime FulfilledDate,
    string? TrackingNumber
);

public record OrderCancelledEvent(
    Guid OrderId,
    string Reason
);

public record OrderStatusChangedEvent(
    Guid OrderId,
    string OldStatus,
    string NewStatus
);
