using MyApp.Shared.Domain.DTOs;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    /// <summary>Data transfer object for Order.</summary>
    public record OrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        /// <summary>Gets the date the order was created.</summary>
        public DateTime OrderDate { get; init; } = default;
        /// <summary>Gets the unique order number.</summary>
        public string OrderNumber { get; init; } = string.Empty;
        /// <summary>Gets the order status.</summary>
        public string Status { get; init; } = string.Empty;
        /// <summary>Gets the order type.</summary>
        public string Type { get; init; } = string.Empty;

        // Logistic Points
        /// <summary>Gets the source location ID.</summary>
        public Guid? SourceId { get; init; }
        /// <summary>Gets the target location ID.</summary>
        public Guid? TargetId { get; init; }

        // External Reference
        /// <summary>Gets the external order ID.</summary>
        public Guid? ExternalOrderId { get; init; }

        /// <summary>Gets the collection of order line DTOs.</summary>
        public List<OrderLineDto> Lines { get; init; } = new();
    }
}
