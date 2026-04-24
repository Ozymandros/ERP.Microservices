using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for contact information.</summary>
public sealed record ContactDto(
    /// <summary>The contact ID.</summary>
    Guid Id,
    /// <summary>The account ID.</summary>
    Guid AccountId,
    /// <summary>The contact's full name.</summary>
    string FullName,
    /// <summary>The contact's email address.</summary>
    string? Email,
    /// <summary>The contact's phone number.</summary>
    string? Phone,
    /// <summary>The contact's job title.</summary>
    string? Title,
    /// <summary>Whether this is the primary contact.</summary>
    bool IsPrimary,
    /// <summary>Whether the contact is active.</summary>
    bool IsActive,
    /// <summary>The creation date.</summary>
    DateTime CreatedAt,
    /// <summary>The last update date.</summary>
    DateTime? UpdatedAt
);

/// <summary>Data transfer object for creating a contact.</summary>
public sealed record CreateContactDto(
    /// <summary>The account ID.</summary>
    [Required] Guid AccountId,
    /// <summary>The contact's full name.</summary>
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    /// <summary>The contact's email address.</summary>
    [EmailAddress, StringLength(255)] string? Email,
    /// <summary>The contact's phone number.</summary>
    [Phone, StringLength(32)] string? Phone,
    /// <summary>The contact's job title.</summary>
    [StringLength(128)] string? Title,
    /// <summary>Whether this should be the primary contact.</summary>
    bool IsPrimary = false
);

/// <summary>Data transfer object for updating a contact.</summary>
public sealed record UpdateContactDto(
    /// <summary>The contact's full name.</summary>
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    /// <summary>The contact's email address.</summary>
    [EmailAddress, StringLength(255)] string? Email,
    /// <summary>The contact's phone number.</summary>
    [Phone, StringLength(32)] string? Phone,
    /// <summary>The contact's job title.</summary>
    [StringLength(128)] string? Title
);

/// <summary>Data transfer object for setting a primary contact.</summary>
public sealed record SetPrimaryContactDto(
    /// <summary>The contact ID to set as primary.</summary>
    [Required] Guid ContactId
);

