using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for cancelling an order
/// </summary>
public record CancelOrderDto
{
    /// <summary>Gets or sets the unique identifier of the order to cancel.</summary>
    [Required]
    public Guid OrderId { get; init; }

    /// <summary>Gets or sets the reason for cancellation.</summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
