using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for creating an order with automatic stock reservation
/// </summary>
public record CreateOrderWithReservationDto
{
    [Required]
    public string OrderNumber { get; init; } = string.Empty;

    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    public Guid WarehouseId { get; init; }

    [Required]
    public DateTime OrderDate { get; init; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? ShippingAddress { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one line")]
    public List<CreateOrderLineDto> Lines { get; init; } = new();
}

/// <summary>
/// DTO for creating an order line
/// </summary>
public record CreateOrderLineDto
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; init; }
}
