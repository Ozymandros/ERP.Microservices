using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

public sealed record OpportunityLineDto(
    Guid Id,
    Guid OpportunityId,
    Guid? ProductId,
    string? Sku,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record CreateOpportunityLineDto(
    [Required, StringLength(500, MinimumLength = 1)] string Description,
    [Range(0.0001, double.MaxValue)] decimal Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    [Range(0, 1)] decimal DiscountPercent = 0m,
    Guid? ProductId = null,
    [StringLength(64)] string? Sku = null
);

public sealed record UpdateOpportunityLineDto(
    [Required, StringLength(500, MinimumLength = 1)] string Description,
    [Range(0.0001, double.MaxValue)] decimal Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    [Range(0, 1)] decimal DiscountPercent = 0m,
    Guid? ProductId = null,
    [StringLength(64)] string? Sku = null
);

