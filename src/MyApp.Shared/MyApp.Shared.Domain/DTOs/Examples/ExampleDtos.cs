namespace MyApp.Shared.Domain.DTOs.Examples
{
    // Example: Simple DTO with Guid ID
    /// <summary>
    /// User dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public record UserDto(Guid Id) : BaseGuidDto(Id)
    {
        /// <summary>Gets or sets Name.</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Gets or sets Email.</summary>
        public string Email { get; init; } = string.Empty;
    }

    // Example: DTO with audit trail and Guid ID
    // Inherited properties (CreatedAt, CreatedBy, etc.) are not redefined
    /// <summary>
    /// Order dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public record OrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        /// <summary>Gets or sets Order Number.</summary>
        public string OrderNumber { get; init; } = string.Empty;
        /// <summary>Gets or sets Total.</summary>
        public decimal Total { get; init; } = 0;
        /// <summary>Gets or sets Status.</summary>
        public string Status { get; init; } = string.Empty;
    }

    // Example: DTO with integer ID
    /// <summary>
    /// Category dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public record CategoryDto(int Id) : BaseIntDto(Id)
    {
        /// <summary>Gets or sets Name.</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Gets or sets Description.</summary>
        public string Description { get; init; } = string.Empty;
    }

    // Example: DTO with audit trail and integer ID
    // Inherited properties (CreatedAt, CreatedBy, etc.) are not redefined
    /// <summary>
    /// Product dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public record ProductDto(int Id) : AuditableIntDto(Id)
    {
        /// <summary>Gets or sets Name.</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Gets or sets Description.</summary>
        public string Description { get; init; } = string.Empty;
        /// <summary>Gets or sets Price.</summary>
        public decimal Price { get; init; } = 0;
        /// <summary>Gets or sets Category Id.</summary>
        public int CategoryId { get; init; } = 0;
    }
}
