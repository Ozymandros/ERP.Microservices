using MyApp.Shared.Domain.DTOs;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderLineDto : AuditableGuidDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; init; }
        
        [Range(0.0, double.MaxValue)]
        public decimal UnitPrice { get; init; }
        
        public decimal LineTotal { get; init; }
    }
}
