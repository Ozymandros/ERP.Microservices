using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record CreateUpdateOrderDto
    {
        [Required]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Required]
        public Guid CustomerId { get; set; }
        
        [Required]
        public DateTime OrderDate { get; set; }
        
        [Required]
        public List<OrderLineDto> Lines { get; set; } = new();
    }
}
