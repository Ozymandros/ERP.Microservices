using MyApp.Shared.Domain.DTOs;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record OrderLineDto(
        Guid Id,
        DateTime CreatedAt = default,
        string CreatedBy = "",
        DateTime? UpdatedAt = null,
        string? UpdatedBy = null,
        Guid ProductId = default,
        [property: Range(1, int.MaxValue)]
        int Quantity = 1,
        [property: Range(0.0, double.MaxValue)]
        decimal UnitPrice = 0,
        decimal LineTotal = 0
    ) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
}
