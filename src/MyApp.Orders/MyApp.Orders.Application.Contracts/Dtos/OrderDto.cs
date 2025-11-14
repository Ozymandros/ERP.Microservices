using MyApp.Shared.Domain.DTOs;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderDto : AuditableGuidDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<OrderLineDto> Lines { get; set; } = new();
    }
}
