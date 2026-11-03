using System.ComponentModel.DataAnnotations;
using MyApp.Projects.Domain.Entities;
using MyApp.Shared.Domain.DTOs;

namespace MyApp.Projects.Application.Contracts.DTOs;

/// <summary>Resource read model.</summary>
public record ResourceDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the resource number.</summary>
    public string ResourceNo { get; init; } = string.Empty;
    /// <summary>Gets the name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the type.</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>Gets the group id.</summary>
    public Guid? ResourceGroupId { get; init; }
    /// <summary>Gets the group code.</summary>
    public string? ResourceGroupCode { get; init; }
    /// <summary>Gets the linked user id.</summary>
    public Guid? UserId { get; init; }
    /// <summary>Gets the base unit of measure.</summary>
    public string BaseUnitOfMeasure { get; init; } = string.Empty;
    /// <summary>Gets the unit cost (LCY).</summary>
    public decimal UnitCost { get; init; }
    /// <summary>Gets the unit price (LCY).</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Gets the daily capacity.</summary>
    public decimal DailyCapacity { get; init; }
    /// <summary>Gets whether the resource is blocked.</summary>
    public bool Blocked { get; init; }
}

/// <summary>Payload to create or update a resource.</summary>
public record CreateUpdateResourceDto
{
    /// <summary>Gets the resource number (unique).</summary>
    [Required, StringLength(20, MinimumLength = 1)]
    public string ResourceNo { get; init; } = string.Empty;
    /// <summary>Gets the name.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the type.</summary>
    public ResourceType Type { get; init; } = ResourceType.Person;
    /// <summary>Gets the group id.</summary>
    public Guid? ResourceGroupId { get; init; }
    /// <summary>Gets the linked user id.</summary>
    public Guid? UserId { get; init; }
    /// <summary>Gets the base unit of measure.</summary>
    [Required, StringLength(20, MinimumLength = 1)]
    public string BaseUnitOfMeasure { get; init; } = "HOUR";
    /// <summary>Gets the unit cost (LCY).</summary>
    [Range(0, 999999999)]
    public decimal UnitCost { get; init; }
    /// <summary>Gets the unit price (LCY).</summary>
    [Range(0, 999999999)]
    public decimal UnitPrice { get; init; }
    /// <summary>Gets the daily capacity.</summary>
    [Range(0, 24)]
    public decimal DailyCapacity { get; init; } = 8m;
    /// <summary>Gets whether the resource is blocked.</summary>
    public bool Blocked { get; init; }
}

/// <summary>Resource group read model.</summary>
public record ResourceGroupDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the code.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets the name.</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>Payload to create or update a resource group.</summary>
public record CreateUpdateResourceGroupDto
{
    /// <summary>Gets the code (unique).</summary>
    [Required, StringLength(20, MinimumLength = 1)]
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets the name.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
}

/// <summary>Currency read model.</summary>
public record CurrencyDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the ISO code.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets the description.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the symbol.</summary>
    public string? Symbol { get; init; }
    /// <summary>Gets whether this is the local currency.</summary>
    public bool IsLocalCurrency { get; init; }
    /// <summary>Gets the exchange rates (newest first).</summary>
    public List<ExchangeRateDto> ExchangeRates { get; init; } = new();
}

/// <summary>Payload to create or update a currency.</summary>
public record CreateUpdateCurrencyDto
{
    /// <summary>Gets the ISO 4217 code.</summary>
    [Required, StringLength(3, MinimumLength = 3)]
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets the description.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the symbol.</summary>
    [StringLength(5)]
    public string? Symbol { get; init; }
}

/// <summary>Exchange rate read model.</summary>
public record ExchangeRateDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the currency id.</summary>
    public Guid CurrencyId { get; init; }
    /// <summary>Gets the starting date.</summary>
    public DateTime StartingDate { get; init; }
    /// <summary>Gets the rate (LCY per 1 unit).</summary>
    public decimal RateToLocal { get; init; }
}

/// <summary>Payload to add an exchange rate.</summary>
public record CreateExchangeRateDto
{
    /// <summary>Gets the starting date.</summary>
    [Required]
    public DateTime StartingDate { get; init; }
    /// <summary>Gets the rate (LCY per 1 unit of the currency).</summary>
    [Range(typeof(decimal), "0.00000001", "999999999")]
    public decimal RateToLocal { get; init; }
}
