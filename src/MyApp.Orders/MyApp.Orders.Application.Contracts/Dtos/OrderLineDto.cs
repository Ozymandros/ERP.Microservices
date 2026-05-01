using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    /// <summary>Data transfer object for OrderLine.</summary>
    public record OrderLineDto(Guid Id) : AuditableDto<Guid>(Id)
    {
        /// <summary>Gets the product ID.</summary>
        public Guid ProductId { get; init; } = default;

        /// <summary>Gets the ordered quantity.</summary>
        [Range(1, int.MaxValue)]
        public int Quantity { get; init; } = 1;

        /// <summary>Gets the picked quantity.</summary>
        public int PickedQuantity { get; init; } = 0;
        /// <summary>Gets a value indicating whether this line is fulfilled.</summary>
        public bool IsFulfilled { get; init; } = false;
    }
}
