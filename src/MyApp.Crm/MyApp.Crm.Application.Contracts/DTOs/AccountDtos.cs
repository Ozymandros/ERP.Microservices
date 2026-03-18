using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

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

public sealed record UpsertAccountDto(
    [Required] Guid CustomerId,
    [Required, StringLength(255, MinimumLength = 1)] string Name,
    string? TaxId,
    string? BillingAddress,
    string? ShippingAddress,
    DateTimeOffset? SyncedAt
);

public sealed record UpdateAccountOwnerDto(
    [Required, StringLength(128, MinimumLength = 1)] string OwnerUsername
);

