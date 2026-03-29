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

public record SalesCustomerCreatedEvent(
    Guid CustomerId,
    string Name,
    string Email
);

public record SalesCustomerUpdatedEvent(
    Guid CustomerId,
    string Name,
    string Email
);
