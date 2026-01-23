using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderLineDto(Guid Id) : AuditableGuidDto(Id)
    {
        public Guid ProductId { get; init; } = default;

        [Range(1, int.MaxValue)]
        public int Quantity { get; init; } = 1;
        
        public int PickedQuantity { get; init; } = 0;
        public bool IsFulfilled { get; init; } = false;
    }
}
