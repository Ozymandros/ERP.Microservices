using MyApp.Shared.Domain.DTOs;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        public DateTime OrderDate { get; init; } = default;
        public string OrderNumber { get; init; } = string.Empty;
        public Guid CustomerId { get; init; } = default;
        public string Status { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; } = 0;
        public List<OrderLineDto> Lines { get; init; } = new();
    }
}
