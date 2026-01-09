namespace MyApp.Shared.Domain.Exceptions;

public class OrderFulfillmentException : Exception
{
    public Guid OrderId { get; }

    public OrderFulfillmentException(Guid orderId, string message)
        : base($"Order {orderId} cannot be fulfilled: {message}")
    {
        OrderId = orderId;
    }

    public OrderFulfillmentException(string message) : base(message)
    {
    }

    public OrderFulfillmentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
