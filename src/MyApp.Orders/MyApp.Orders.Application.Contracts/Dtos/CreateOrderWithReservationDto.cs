using MyApp.Orders.Domain;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for creating an operational order with automatic stock reservation
/// </summary>
public record CreateOrderWithReservationDto
{
    [Required]
    public string OrderNumber { get; init; } = string.Empty;

    [Required]
    public OrderType Type { get; init; }
    
    public Guid? SourceId { get; init; }
    public Guid? TargetId { get; init; }
    public Guid? ExternalOrderId { get; init; }

    [Required]
    public Guid WarehouseId { get; init; }

    [Required]
    public DateTime OrderDate { get; init; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? DestinationAddress { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one line")]
    public List<CreateOrderLineDto> Lines { get; init; } = new();
}
