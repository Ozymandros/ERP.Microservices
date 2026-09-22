using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Validators;

/// <summary>
/// Provides Stock Validators functionality.
/// </summary>
public static class StockValidators
{
    /// <summary>Validate Reservation.</summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="availableQuantity">The available Quantity.</param>
    public static ValidationResult? ValidateReservation(int quantity, int availableQuantity)
    {
        if (quantity <= 0)
            return new ValidationResult("Reservation quantity must be greater than zero");

        if (quantity > availableQuantity)
            return new ValidationResult($"Cannot reserve {quantity} units. Only {availableQuantity} available");

        return ValidationResult.Success;
    }

    /// <summary>Validate Transfer.</summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="availableQuantity">The available Quantity.</param>
    public static ValidationResult? ValidateTransfer(int quantity, int availableQuantity)
    {
        if (quantity <= 0)
            return new ValidationResult("Transfer quantity must be greater than zero");

        if (quantity > availableQuantity)
            return new ValidationResult($"Cannot transfer {quantity} units. Only {availableQuantity} available");

        return ValidationResult.Success;
    }

    /// <summary>Validate Adjustment.</summary>
    /// <param name="quantityChange">The quantity Change.</param>
    /// <param name="reason">The reason.</param>
    public static ValidationResult? ValidateAdjustment(int quantityChange, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new ValidationResult("Adjustment must have a valid reason");

        return ValidationResult.Success;
    }

    /// <summary>Validate Warehouse Stock.</summary>
    /// <param name="availableQuantity">The available Quantity.</param>
    /// <param name="reservedQuantity">The reserved Quantity.</param>
    /// <param name="onOrderQuantity">The on Order Quantity.</param>
    public static ValidationResult? ValidateWarehouseStock(int availableQuantity, int reservedQuantity, int onOrderQuantity)
    {
        if (availableQuantity < 0)
            return new ValidationResult("Available quantity cannot be negative");

        if (reservedQuantity < 0)
            return new ValidationResult("Reserved quantity cannot be negative");

        if (onOrderQuantity < 0)
            return new ValidationResult("On-order quantity cannot be negative");

        return ValidationResult.Success;
    }
}
