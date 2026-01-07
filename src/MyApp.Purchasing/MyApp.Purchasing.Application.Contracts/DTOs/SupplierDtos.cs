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
    [property: Required(ErrorMessage = "Name is required")]
    [property: StringLength(255, MinimumLength = 1)]
    string Name,

    [property: Required(ErrorMessage = "ContactName is required")]
    [property: StringLength(255, MinimumLength = 1)]
    string ContactName,

    [property: Required(ErrorMessage = "Email is required")]
    [property: EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [property: StringLength(255)]
    string Email,

    [property: Phone(ErrorMessage = "PhoneNumber must be a valid phone number")]
    [property: StringLength(20)]
    string PhoneNumber = "",

    [property: StringLength(500)]
    string Address = ""
);
