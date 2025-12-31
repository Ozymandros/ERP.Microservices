using MyApp.Shared.Domain.DTOs;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderDto(
        Guid Id,
        DateTime CreatedAt = default,
        string CreatedBy = "",
        DateTime? UpdatedAt = null,
        string? UpdatedBy = null,
        string OrderNumber = "",
        Guid CustomerId = default,
        DateTime OrderDate = default,
        string Status = "",
        decimal TotalAmount = 0,
        List<OrderLineDto>? Lines = null
    ) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    {
        public List<OrderLineDto> Lines { get; set; } = Lines ?? new();
    }
}
