using MyApp.Orders.Domain;
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
        public DateTime OrderDate { get; init; } = DateTime.UtcNow;
        
        [Required]
        public OrderType Type { get; init; }
        
        public Guid? SourceId { get; init; }
        public Guid? TargetId { get; init; }
        public Guid? ExternalOrderId { get; init; }
        
        public Guid? WarehouseId { get; init; }

        [Required]
        public List<OrderLineDto> Lines { get; init; } = new();
    }
}
