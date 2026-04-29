namespace MyApp.Orders.Domain.Entities
{
    /// <summary>Represents the status of a stock reservation.</summary>
    public enum ReservationStatus
    {
        /// <summary>Stock has been reserved.</summary>
        Reserved,
        /// <summary>Reserved stock has been fulfilled.</summary>
        Fulfilled,
        /// <summary>Reservation has expired.</summary>
        Expired,
        /// <summary>Reservation has been cancelled.</summary>
        Cancelled
    }
}
