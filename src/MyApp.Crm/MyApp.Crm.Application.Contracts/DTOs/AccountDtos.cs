using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for account information.</summary>
/// <param name="Id">The id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Name">The name.</param>
/// <param name="TaxId">The tax Id.</param>
/// <param name="BillingAddress">The billing Address.</param>
/// <param name="ShippingAddress">The shipping Address.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="OwnerUsername">The owner Username.</param>
/// <param name="LastSyncedAt">The last Synced At.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
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
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Name">The name.</param>
/// <param name="TaxId">The tax Id.</param>
/// <param name="BillingAddress">The billing Address.</param>
/// <param name="ShippingAddress">The shipping Address.</param>
/// <param name="SyncedAt">The synced At.</param>
public sealed record UpsertAccountDto(
    [Required] Guid CustomerId,
    [Required, StringLength(255, MinimumLength = 1)] string Name,
    string? TaxId,
    string? BillingAddress,
    string? ShippingAddress,
    DateTimeOffset? SyncedAt
);

/// <summary>Data transfer object for updating an account owner.</summary>
/// <param name="OwnerUsername">The owner Username.</param>
public sealed record UpdateAccountOwnerDto(
    [Required, StringLength(128, MinimumLength = 1)] string OwnerUsername
);

