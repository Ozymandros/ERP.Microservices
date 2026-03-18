using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Opportunities;

public sealed class OpportunityLine(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid OpportunityId { get; private set; }

    public Guid? ProductId { get; private set; }
    public string? Sku { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    /// <summary>
    /// 0..1 (e.g. 0.10 = 10% discount)
    /// </summary>
    public decimal DiscountPercent { get; private set; }

    public decimal LineTotal => Math.Round(Quantity * UnitPrice * (1m - DiscountPercent), 2, MidpointRounding.AwayFromZero);

    public OpportunityLine(
        Guid id,
        Guid opportunityId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        Guid? productId = null,
        string? sku = null) : this(id)
    {
        if (opportunityId == Guid.Empty) throw new ArgumentException("OpportunityId is required.", nameof(opportunityId));
        OpportunityId = opportunityId;
        Update(description, quantity, unitPrice, discountPercent, productId, sku);
    }

    public void Update(
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        Guid? productId = null,
        string? sku = null)
    {
        Description = NormalizeRequired(description, nameof(description));
        Quantity = quantity > 0m ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than 0.");
        UnitPrice = unitPrice >= 0m ? unitPrice : throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");

        if (discountPercent is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "DiscountPercent must be between 0 and 1.");
        DiscountPercent = discountPercent;

        ProductId = productId;
        Sku = NormalizeOptional(sku);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

