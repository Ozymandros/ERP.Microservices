using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record CreateUpdateOrderDto
    {
        [Required]
        public string OrderNumber { get; init; } = string.Empty;

        [Required]
        public Guid CustomerId { get; init; }

        [Required]
        public DateTime OrderDate { get; init; } = DateTime.UtcNow;

        [Required]
        public List<OrderLineDto> Lines { get; init; } = new();
    }
}
