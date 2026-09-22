using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>
/// DTO for creating or updating an operational order line
/// </summary>
public record CreateOrderLineDto
{
    /// <summary>Gets or sets the product ID for this order line.</summary>
    [Required]
    public Guid ProductId { get; init; }

    /// <summary>Gets or sets the quantity to order; must be at least 1.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }
}
