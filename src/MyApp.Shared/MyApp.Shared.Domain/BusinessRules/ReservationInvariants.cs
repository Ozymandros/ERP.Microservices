namespace MyApp.Shared.Domain.BusinessRules;

public static class ReservationInvariants
{
    public static readonly TimeSpan DefaultReservationDuration = TimeSpan.FromHours(24);

    public static bool IsReservationExpired(DateTime reservedUntil)
    {
        return DateTime.UtcNow > reservedUntil;
    }

    public static bool IsQuantityValid(int quantity)
    {
        return quantity > 0;
    }

    public static void ValidateReservation(int quantity, int availableQuantity, DateTime reservedUntil)
    {
        if (!IsQuantityValid(quantity))
            throw new InvalidOperationException("Reservation quantity must be greater than zero");
        
        if (quantity > availableQuantity)
            throw new InvalidOperationException($"Cannot reserve {quantity} units. Only {availableQuantity} available");
        
        if (IsReservationExpired(reservedUntil))
            throw new InvalidOperationException("Reservation expiry date must be in the future");
    }

    public static DateTime CalculateReservationExpiry()
    {
        return DateTime.UtcNow.Add(DefaultReservationDuration);
    }
}
