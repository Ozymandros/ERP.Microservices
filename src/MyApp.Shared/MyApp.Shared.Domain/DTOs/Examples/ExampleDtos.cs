namespace MyApp.Shared.Domain.DTOs.Examples
{
    // Example: Simple DTO with Guid ID
    public record UserDto(Guid Id) : BaseGuidDto(Id)
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }

    // Example: DTO with audit trail and Guid ID
    // Propietats heretades (CreatedAt, CreatedBy, etc.) no es redefinixen
    public record OrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        public string OrderNumber { get; init; } = string.Empty;
        public decimal Total { get; init; } = 0;
        public string Status { get; init; } = string.Empty;
    }

    // Example: DTO with integer ID
    public record CategoryDto(int Id) : BaseIntDto(Id)
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    // Example: DTO with audit trail and integer ID
    // Propietats heretades (CreatedAt, CreatedBy, etc.) no es redefinixen
    public record ProductDto(int Id) : AuditableIntDto(Id)
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; } = 0;
        public int CategoryId { get; init; } = 0;
    }
}