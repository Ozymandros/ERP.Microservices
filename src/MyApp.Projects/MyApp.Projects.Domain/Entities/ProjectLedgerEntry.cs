using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>
/// Immutable project ledger entry (Business Central "Project Ledger Entry").
/// Usage entries carry cost (LCY) and price; sale entries carry invoiced amounts.
/// Corrections are made with new (negative) entries, never by updating existing ones.
/// </summary>
public class ProjectLedgerEntry(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the sequential entry number within the project.</summary>
    public int EntryNo { get; set; }

    /// <summary>Gets or sets the entry type.</summary>
    public ProjectLedgerEntryType EntryType { get; set; }

    /// <summary>Gets or sets the posting date.</summary>
    public DateTime PostingDate { get; set; }

    /// <summary>Gets or sets the project id.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the task id.</summary>
    public Guid ProjectTaskId { get; set; }

    /// <summary>Gets or sets the related planning line id, if any.</summary>
    public Guid? PlanningLineId { get; set; }

    /// <summary>Gets or sets the kind.</summary>
    public ProjectLineKind Kind { get; set; }

    /// <summary>Gets or sets the resource id.</summary>
    public Guid? ResourceId { get; set; }

    /// <summary>Gets or sets the product id.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity (negative for corrections/reversals).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the unit cost in LCY.</summary>
    public decimal UnitCostLcy { get; set; }

    /// <summary>Gets or sets the total cost in LCY.</summary>
    public decimal TotalCostLcy { get; set; }

    /// <summary>Gets or sets the unit price in project currency.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the line amount in project currency.</summary>
    public decimal LineAmount { get; set; }

    /// <summary>Gets or sets the line amount in LCY.</summary>
    public decimal LineAmountLcy { get; set; }

    /// <summary>Gets or sets the project currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange rate (LCY per 1 unit of currency) at posting.</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Gets or sets the source document type.</summary>
    public ProjectSourceDocument SourceDocument { get; set; }

    /// <summary>Gets or sets the source document id (order id, invoice id, ...).</summary>
    public Guid? SourceDocumentId { get; set; }

    /// <summary>Gets or sets the source document number.</summary>
    public string? SourceDocumentNo { get; set; }

    /// <summary>Calculates cost and amounts with standard rounding.</summary>
    public void Calculate()
    {
        TotalCostLcy = Math.Round(Quantity * UnitCostLcy, 2, MidpointRounding.AwayFromZero);
        LineAmount = Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
        LineAmountLcy = Math.Round(LineAmount * ExchangeRate, 2, MidpointRounding.AwayFromZero);
    }
}
