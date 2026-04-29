using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for account information.</summary>
public sealed record AccountDto(
    Guid Id,
    Guid CustomerId,
    string Name,
    string? TaxId,
    string? BillingAddress,
    string? ShippingAddress,
    bool IsActive,
    string? OwnerUsername,
    DateTimeOffset LastSyncedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Data transfer object for creating or updating an account from a sales snapshot.</summary>
public sealed record UpsertAccountDto(
    [Required] Guid CustomerId,
    [Required, StringLength(255, MinimumLength = 1)] string Name,
    string? TaxId,
    string? BillingAddress,
    string? ShippingAddress,
    DateTimeOffset? SyncedAt
);

/// <summary>Data transfer object for updating an account owner.</summary>
public sealed record UpdateAccountOwnerDto(
    [Required, StringLength(128, MinimumLength = 1)] string OwnerUsername
);

