using MyApp.Shared.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

public record SupplierDto(Guid Id) : AuditableGuidDto(Id)
{
    public string Name { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}

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
