using MyApp.Orders.Domain;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for creating an operational order with automatic stock reservation
/// </summary>
public record CreateOrderWithReservationDto
{
    /// <summary>Gets or sets the unique order number.</summary>
    [Required]
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>Gets or sets the operational order type (Transfer, Inbound, Outbound, or Return).</summary>
    [Required]
    public OrderType Type { get; init; }

    /// <summary>Gets or sets the source location ID for the order movement.</summary>
    public Guid? SourceId { get; init; }

    /// <summary>Gets or sets the target location ID for the order movement.</summary>
    public Guid? TargetId { get; init; }

    /// <summary>Gets or sets the external order ID linking to a SalesOrder or PurchaseOrder.</summary>
    public Guid? ExternalOrderId { get; init; }

    /// <summary>Gets or sets the warehouse ID from which stock is reserved.</summary>
    [Required]
    public Guid WarehouseId { get; init; }

    /// <summary>Gets or sets the date the order was placed.</summary>
    [Required]
    public DateTime OrderDate { get; init; } = DateTime.UtcNow;

    /// <summary>Gets or sets the destination address for the shipment.</summary>
    [MaxLength(500)]
    public string? DestinationAddress { get; init; }

    /// <summary>Gets or sets the collection of order lines; must contain at least one entry.</summary>
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one line")]
    public List<CreateOrderLineDto> Lines { get; init; } = new();
}
