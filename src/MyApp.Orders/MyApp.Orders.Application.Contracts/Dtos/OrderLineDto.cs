using MyApp.Shared.Domain.DTOs;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderLineDto : AuditableGuidDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Range(0.0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
        
        public decimal LineTotal { get; set; }
    }
}
