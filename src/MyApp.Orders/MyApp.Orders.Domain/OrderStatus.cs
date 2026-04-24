namespace MyApp.Orders.Domain
{
    /// <summary>Represents the status of an order.</summary>
    public enum OrderStatus
    {
        /// <summary>Order is in draft state.</summary>
        Draft,
        /// <summary>Order has been approved.</summary>
        Approved,
        /// <summary>Order is in transit.</summary>
        InTransit,
        /// <summary>Order has been received.</summary>
        Received,
        /// <summary>Order is completed.</summary>
        Completed,
        /// <summary>Order has been cancelled.</summary>
        Cancelled
    }
}
