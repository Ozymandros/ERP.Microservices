using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for cancelling an order
/// </summary>
public record CancelOrderDto
{
    [Required]
    public Guid OrderId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
