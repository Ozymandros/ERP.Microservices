using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Opportunities;

/// <summary>Represents a line item within a CRM opportunity, capturing product, pricing, and quantity details.</summary>
/// <param name="id">The id.</param>
public sealed class OpportunityLine(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the opportunity ID this line belongs to.</summary>
    public Guid OpportunityId { get; private set; }

    /// <summary>Gets the optional product ID associated with this line.</summary>
    public Guid? ProductId { get; private set; }
    /// <summary>Gets the optional stock-keeping unit identifier.</summary>
    public string? Sku { get; private set; }

    /// <summary>Gets the description of the line item.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Gets the quantity for this line item.</summary>
    public decimal Quantity { get; private set; }
    /// <summary>Gets the unit price for this line item.</summary>
    public decimal UnitPrice { get; private set; }
    /// <summary>
    /// 0..1 (e.g. 0.10 = 10% discount)
    /// </summary>
    public decimal DiscountPercent { get; private set; }

    /// <summary>Gets the calculated line total after applying quantity, unit price, and discount.</summary>
    public decimal LineTotal => Math.Round(Quantity * UnitPrice * (1m - DiscountPercent), 2, MidpointRounding.AwayFromZero);

    /// <summary>Initializes a new instance of the OpportunityLine class.</summary>
    /// Initializes a new instance of the OpportunityLine class.
    /// <param name="id">The id.</param>
    /// <param name="opportunityId">The opportunity Id.</param>
    /// <param name="description">The description.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="discountPercent">The discount Percent.</param>
    /// <param name="productId">The product Id.</param>
    /// <param name="sku">The sku.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="opportunityId"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="quantity"/>, <paramref name="unitPrice"/>, or <paramref name="discountPercent"/> are out of valid range.</exception>
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

    /// <summary>Updates the line item details.</summary>
    /// Updates an existing item.
    /// <param name="description">The description.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="discountPercent">The discount Percent.</param>
    /// <param name="productId">The product Id.</param>
    /// <param name="sku">The sku.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="quantity"/>, <paramref name="unitPrice"/>, or <paramref name="discountPercent"/> are out of valid range.</exception>
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

