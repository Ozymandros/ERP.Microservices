using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>Data transfer object for account information.</summary>
public sealed record AccountDto(
    /// <summary>The account ID.</summary>
    Guid Id,
    /// <summary>The customer ID.</summary>
    Guid CustomerId,
    /// <summary>The account name.</summary>
    string Name,
    /// <summary>The tax ID.</summary>
    string? TaxId,
    /// <summary>The billing address.</summary>
    string? BillingAddress,
    /// <summary>The shipping address.</summary>
    string? ShippingAddress,
    /// <summary>Whether the account is active.</summary>
    bool IsActive,
    /// <summary>The owner username.</summary>
    string? OwnerUsername,
    /// <summary>The last synchronization time.</summary>
    DateTimeOffset LastSyncedAt,
    /// <summary>The creation date.</summary>
    DateTime CreatedAt,
    /// <summary>The last update date.</summary>
    DateTime? UpdatedAt
);

/// <summary>Data transfer object for creating or updating an account from a sales snapshot.</summary>
public sealed record UpsertAccountDto(
    /// <summary>The customer ID.</summary>
    [Required] Guid CustomerId,
    /// <summary>The account name.</summary>
    [Required, StringLength(255, MinimumLength = 1)] string Name,
    /// <summary>The tax ID.</summary>
    string? TaxId,
    /// <summary>The billing address.</summary>
    string? BillingAddress,
    /// <summary>The shipping address.</summary>
    string? ShippingAddress,
    /// <summary>The synchronization time.</summary>
    DateTimeOffset? SyncedAt
);

/// <summary>Data transfer object for updating an account owner.</summary>
public sealed record UpdateAccountOwnerDto(
    /// <summary>The new owner username.</summary>
    [Required, StringLength(128, MinimumLength = 1)] string OwnerUsername
);

