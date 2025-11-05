using MyApp.Shared.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Purchasing.Application.Contracts.DTOs;

public class SupplierDto : AuditableGuidDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class CreateUpdateSupplierDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "ContactName is required")]
    [StringLength(255, MinimumLength = 1)]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "PhoneNumber must be a valid phone number")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string Address { get; set; } = string.Empty;
}
