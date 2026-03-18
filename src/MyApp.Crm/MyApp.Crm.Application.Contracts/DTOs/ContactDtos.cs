using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

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

public sealed record CreateContactDto(
    [Required] Guid AccountId,
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [EmailAddress, StringLength(255)] string? Email,
    [Phone, StringLength(32)] string? Phone,
    [StringLength(128)] string? Title,
    bool IsPrimary = false
);

public sealed record UpdateContactDto(
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [EmailAddress, StringLength(255)] string? Email,
    [Phone, StringLength(32)] string? Phone,
    [StringLength(128)] string? Title
);

public sealed record SetPrimaryContactDto(
    [Required] Guid ContactId
);

