using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for contact information.</summary>
/// <param name="Id">The id.</param>
/// <param name="AccountId">The account Id.</param>
/// <param name="FullName">The full Name.</param>
/// <param name="Email">The email.</param>
/// <param name="Phone">The phone.</param>
/// <param name="Title">The title.</param>
/// <param name="IsPrimary">The is Primary.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
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
/// <param name="AccountId">The account Id.</param>
/// <param name="FullName">The full Name.</param>
/// <param name="Email">The email.</param>
/// <param name="Phone">The phone.</param>
/// <param name="Title">The title.</param>
/// <param name="IsPrimary">The is Primary.</param>
public sealed record CreateContactDto(
    [Required] Guid AccountId,
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [EmailAddress, StringLength(255)] string? Email,
    [Phone, StringLength(32)] string? Phone,
    [StringLength(128)] string? Title,
    bool IsPrimary = false
);

/// <summary>Data transfer object for updating a contact.</summary>
/// <param name="FullName">The full Name.</param>
/// <param name="Email">The email.</param>
/// <param name="Phone">The phone.</param>
/// <param name="Title">The title.</param>
public sealed record UpdateContactDto(
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [EmailAddress, StringLength(255)] string? Email,
    [Phone, StringLength(32)] string? Phone,
    [StringLength(128)] string? Title
);

/// <summary>Data transfer object for setting a primary contact.</summary>
/// <param name="ContactId">The contact Id.</param>
public sealed record SetPrimaryContactDto(
    [Required] Guid ContactId
);

