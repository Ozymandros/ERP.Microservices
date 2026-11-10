using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>Currency known to the Projects service (ISO 4217).</summary>
public class Currency(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the ISO 4217 code (unique, upper case).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the symbol.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets the exchange rates.</summary>
    public List<ExchangeRate> ExchangeRates { get; set; } = new();
}

/// <summary>
/// Exchange rate valid from <see cref="StartingDate"/>: how many units of local currency (LCY)
/// equal one unit of the foreign currency.
/// </summary>
public class ExchangeRate(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the currency id.</summary>
    public Guid CurrencyId { get; set; }

    /// <summary>Gets or sets the date from which the rate applies.</summary>
    public DateTime StartingDate { get; set; }

    /// <summary>Gets or sets the rate: LCY per 1 unit of the currency.</summary>
    public decimal RateToLocal { get; set; }

    /// <summary>Gets or sets the currency navigation.</summary>
    public Currency? Currency { get; set; }
}
