using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for contact information.</summary>
public sealed record ContactDto(
    Guid Id,
    Guid AccountId,
    string FullName,
    string? Email,
    string? Phone,
    string? Title,
    bool IsPrimary,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Data transfer object for creating a contact.</summary>
public sealed record CreateContactDto(
    [Required] Guid AccountId,
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [EmailAddress, StringLength(255)] string? Email,
    [Phone, StringLength(32)] string? Phone,
    [StringLength(128)] string? Title,
    bool IsPrimary = false
);

/// <summary>Data transfer object for updating a contact.</summary>
public sealed record UpdateContactDto(
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [EmailAddress, StringLength(255)] string? Email,
    [Phone, StringLength(32)] string? Phone,
    [StringLength(128)] string? Title
);

/// <summary>Data transfer object for setting a primary contact.</summary>
public sealed record SetPrimaryContactDto(
    [Required] Guid ContactId
);

