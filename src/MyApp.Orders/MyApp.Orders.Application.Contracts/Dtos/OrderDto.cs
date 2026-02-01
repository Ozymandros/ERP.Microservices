using MyApp.Shared.Domain.DTOs;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        public DateTime OrderDate { get; init; } = default;
        public string OrderNumber { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        
        // Logistic Points
        public Guid? SourceId { get; init; }
        public Guid? TargetId { get; init; }
        
        // External Reference
        public Guid? ExternalOrderId { get; init; }
        
        public List<OrderLineDto> Lines { get; init; } = new();
    }
}
