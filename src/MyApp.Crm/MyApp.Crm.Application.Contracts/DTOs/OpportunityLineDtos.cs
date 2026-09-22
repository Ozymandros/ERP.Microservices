using System.ComponentModel.DataAnnotations;

namespace MyApp.Crm.Application.Contracts.DTOs;

/// <summary>
/// Opportunity line dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="OpportunityId">The opportunity Id.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="Sku">The sku.</param>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="DiscountPercent">The discount Percent.</param>
/// <param name="LineTotal">The line Total.</param>
/// <param name="CreatedAt">The created At.</param>
/// <param name="UpdatedAt">The updated At.</param>
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

/// <summary>
/// Creates an opportunity line dto.
/// </summary>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="DiscountPercent">The discount Percent.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="Sku">The sku.</param>
public sealed record CreateOpportunityLineDto(
    [Required, StringLength(500, MinimumLength = 1)] string Description,
    [Range(0.0001, double.MaxValue)] decimal Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    [Range(0, 1)] decimal DiscountPercent = 0m,
    Guid? ProductId = null,
    [StringLength(64)] string? Sku = null
);

/// <summary>
/// Updates the opportunity line dto.
/// </summary>
/// <param name="Description">The description.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The unit Price.</param>
/// <param name="DiscountPercent">The discount Percent.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="Sku">The sku.</param>
public sealed record UpdateOpportunityLineDto(
    [Required, StringLength(500, MinimumLength = 1)] string Description,
    [Range(0.0001, double.MaxValue)] decimal Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    [Range(0, 1)] decimal DiscountPercent = 0m,
    Guid? ProductId = null,
    [StringLength(64)] string? Sku = null
);

