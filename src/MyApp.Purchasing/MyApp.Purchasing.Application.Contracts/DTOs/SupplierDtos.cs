using MyApp.Shared.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

/// <summary>
/// Supplier dto.
/// </summary>
/// <param name="Id">The id.</param>
public record SupplierDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets or sets Contact Name.</summary>
    public string ContactName { get; init; } = string.Empty;
    /// <summary>Gets or sets Email.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>Gets or sets Phone Number.</summary>
    public string PhoneNumber { get; init; } = string.Empty;
    /// <summary>Gets or sets Address.</summary>
    public string Address { get; init; } = string.Empty;
}

/// <summary>
/// Creates an update supplier dto.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="ContactName">The contact Name.</param>
/// <param name="Email">The email.</param>
/// <param name="PhoneNumber">The phone Number.</param>
/// <param name="Address">The address.</param>
public record CreateUpdateSupplierDto(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1)]
    string Name,

    [Required(ErrorMessage = "ContactName is required")]
    [StringLength(255, MinimumLength = 1)]
    string ContactName,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(255)]
    string Email,

    [Phone(ErrorMessage = "PhoneNumber must be a valid phone number")]
    [StringLength(20)]
    string PhoneNumber = "",

    [StringLength(500)]
    string Address = ""
);
