using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for fulfilling an order
/// </summary>
public record FulfillOrderDto
{
    [Required]
    public Guid OrderId { get; init; }

    [Required]
    public Guid WarehouseId { get; init; }

    [MaxLength(500)]
    public string? ShippingAddress { get; init; }

    [MaxLength(100)]
    public string? TrackingNumber { get; init; }
}
