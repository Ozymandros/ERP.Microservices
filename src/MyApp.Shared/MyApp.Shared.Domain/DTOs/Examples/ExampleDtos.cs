namespace MyApp.Shared.Domain.DTOs.Examples
{
    // Example: Simple DTO with Guid ID
    public class UserDto : BaseGuidDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Example: DTO with audit trail and Guid ID
    public class OrderDto : AuditableGuidDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Example: DTO with integer ID
    public class CategoryDto : BaseIntDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Example: DTO with audit trail and integer ID
    public class ProductDto : AuditableIntDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
    }
}