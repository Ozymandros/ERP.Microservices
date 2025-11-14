using MyApp.Shared.Domain.DTOs;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderDto : AuditableGuidDto
    {
        public string OrderNumber { get; init; } = string.Empty;
        public Guid CustomerId { get; init; }
        public DateTime OrderDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public List<OrderLineDto> Lines { get; init; } = new();
    }
}
