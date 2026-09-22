namespace MyApp.Shared.Domain.BusinessRules;

/// <summary>
/// Defines business rule invariants for stock reservation processing and validation.
/// </summary>
public static class ReservationInvariants
{
    /// <summary>
    /// Gets the default duration for which a stock reservation remains valid.
    /// </summary>
    public static readonly TimeSpan DefaultReservationDuration = TimeSpan.FromHours(24);

    /// <summary>
    /// Determines whether reservation expired.
    /// </summary>
    /// <param name="reservedUntil">The reserved Until.</param>
    public static bool IsReservationExpired(DateTime reservedUntil)
    {
        return DateTime.UtcNow > reservedUntil;
    }

    /// <summary>
    /// Determines whether quantity valid.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    public static bool IsQuantityValid(int quantity)
    {
        return quantity > 0;
    }

    /// <summary>
    /// Validate reservation.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="availableQuantity">The available Quantity.</param>
    /// <param name="reservedUntil">The reserved Until.</param>
    public static void ValidateReservation(int quantity, int availableQuantity, DateTime reservedUntil)
    {
        if (!IsQuantityValid(quantity))
            throw new InvalidOperationException("Reservation quantity must be greater than zero");

        if (quantity > availableQuantity)
            throw new InvalidOperationException($"Cannot reserve {quantity} units. Only {availableQuantity} available");

        if (IsReservationExpired(reservedUntil))
            throw new InvalidOperationException("Reservation expiry date must be in the future");
    }

    /// <summary>
    /// Calculate reservation expiry.
    /// </summary>
    public static DateTime CalculateReservationExpiry()
    {
        return DateTime.UtcNow.Add(DefaultReservationDuration);
    }
}
