namespace MyApp.Shared.Domain.Events;

public record SalesOrderCreatedEvent(
    Guid SalesOrderId,
    Guid CustomerId,
    string OrderNumber,
    bool IsQuote,
    decimal TotalAmount
);

public record SalesOrderConfirmedEvent(
    Guid SalesOrderId,
    Guid OrderId,
    DateTime ConfirmedDate
);

public record QuoteExpiredEvent(
    Guid SalesOrderId,
    DateTime ExpiryDate
);

public record SalesOrderStatusChangedEvent(
    Guid SalesOrderId,
    string OldStatus,
    string NewStatus
);
