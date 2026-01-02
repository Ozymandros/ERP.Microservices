namespace MyApp.Shared.Domain.DTOs.Examples
{
    // Example: Simple DTO with Guid ID
    public record UserDto(Guid Id, string Name, string Email) : BaseGuidDto(Id);

    // Example: DTO with audit trail and Guid ID
    public record OrderDto(
        Guid Id, 
        DateTime CreatedAt = default, 
        string CreatedBy = "", 
        DateTime? UpdatedAt = null, 
        string? UpdatedBy = null,
        string OrderNumber = "", 
        decimal Total = 0, 
        string Status = "") 
        : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

    // Example: DTO with integer ID
    public record CategoryDto(int Id, string Name, string Description) : BaseIntDto(Id);

    // Example: DTO with audit trail and integer ID
    public record ProductDto(
        int Id, 
        DateTime CreatedAt = default, 
        string CreatedBy = "", 
        DateTime? UpdatedAt = null, 
        string? UpdatedBy = null,
        string Name = "", 
        string Description = "", 
        decimal Price = 0, 
        int CategoryId = 0) 
        : AuditableIntDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
}