using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for fulfilling an order
/// </summary>
public record FulfillOrderDto
{
    /// <summary>Gets or sets the unique identifier of the order to fulfill.</summary>
    [Required]
    public Guid OrderId { get; init; }

    /// <summary>Gets or sets the warehouse ID from which the order is fulfilled.</summary>
    [Required]
    public Guid WarehouseId { get; init; }

    /// <summary>Gets or sets the destination shipping address.</summary>
    [MaxLength(500)]
    public string? ShippingAddress { get; init; }

    /// <summary>Gets or sets the shipment tracking number.</summary>
    [MaxLength(100)]
    public string? TrackingNumber { get; init; }
}
