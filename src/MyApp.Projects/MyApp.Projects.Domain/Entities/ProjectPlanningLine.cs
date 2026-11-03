using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>
/// Project planning line (Business Central "Project Planning Line").
/// Costs are kept in local currency (LCY); prices in the project currency, together with the exchange rate
/// (LCY per 1 unit of project currency) captured when the line was priced.
/// </summary>
public class ProjectPlanningLine(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the owning project id.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the project task id (must be a leaf posting task).</summary>
    public Guid ProjectTaskId { get; set; }

    /// <summary>Gets or sets the line number within the project.</summary>
    public int LineNo { get; set; }

    /// <summary>Gets or sets the line type (budget / billable / both).</summary>
    public PlanningLineType LineType { get; set; } = PlanningLineType.BudgetAndBillable;

    /// <summary>Gets or sets what the line refers to.</summary>
    public ProjectLineKind Kind { get; set; } = ProjectLineKind.Resource;

    /// <summary>Gets or sets the resource id (Kind = Resource).</summary>
    public Guid? ResourceId { get; set; }

    /// <summary>Gets or sets the product id (Kind = Item, owned by Inventory).</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Gets or sets the warehouse to consume items from (Kind = Item, owned by Inventory).</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the unit of measure (e.g. HOUR, PCS).</summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>Gets or sets the planning date.</summary>
    public DateTime PlanningDate { get; set; }

    /// <summary>Gets or sets the planned quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the unit cost in local currency.</summary>
    public decimal UnitCostLcy { get; set; }

    /// <summary>Gets or sets the unit price in project currency.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the line discount percentage (0-100).</summary>
    public decimal LineDiscountPercent { get; set; }

    /// <summary>Gets or sets the project currency code (copied from the project).</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange rate: LCY per 1 unit of <see cref="CurrencyCode"/>.</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Gets or sets the total cost in LCY (Quantity * UnitCostLcy).</summary>
    public decimal TotalCostLcy { get; set; }

    /// <summary>Gets or sets the line amount in project currency (after discount).</summary>
    public decimal LineAmount { get; set; }

    /// <summary>Gets or sets the line amount in LCY.</summary>
    public decimal LineAmountLcy { get; set; }

    /// <summary>Gets or sets the quantity already posted as usage.</summary>
    public decimal QuantityPosted { get; set; }

    /// <summary>Gets or sets the item quantity sent to Orders and not yet fulfilled.</summary>
    public decimal QuantityInConsumption { get; set; }

    /// <summary>Gets or sets the quantity sent to Billing (requested or invoiced).</summary>
    public decimal QuantityTransferredToInvoice { get; set; }

    /// <summary>Gets or sets the quantity invoiced (invoice issued).</summary>
    public decimal QuantityInvoiced { get; set; }

    /// <summary>Gets whether the line is part of the budget.</summary>
    public bool IsBudget => LineType is PlanningLineType.Budget or PlanningLineType.BudgetAndBillable;

    /// <summary>Gets whether the line can be invoiced.</summary>
    public bool IsBillable => LineType is PlanningLineType.Billable or PlanningLineType.BudgetAndBillable;

    /// <summary>Gets the quantity that can still be sent to Billing.</summary>
    public decimal QuantityToInvoice => IsBillable && Kind != ProjectLineKind.Text
        ? Math.Max(0, Quantity - QuantityTransferredToInvoice)
        : 0;

    /// <summary>Gets the quantity still to be used/consumed.</summary>
    public decimal RemainingQuantity => Math.Max(0, Quantity - QuantityPosted - QuantityInConsumption);

    /// <summary>Validates the line consistency.</summary>
    /// <exception cref="ArgumentException">When the line is inconsistent.</exception>
    public void Validate()
    {
        if (Quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");
        if (UnitCostLcy < 0 || UnitPrice < 0)
            throw new ArgumentException("Unit cost and unit price cannot be negative.");
        if (LineDiscountPercent is < 0 or > 100)
            throw new ArgumentException("LineDiscountPercent must be between 0 and 100.");
        if (ExchangeRate <= 0)
            throw new ArgumentException("ExchangeRate must be greater than zero.");

        switch (Kind)
        {
            case ProjectLineKind.Resource when ResourceId is null:
                throw new ArgumentException("ResourceId is required for resource lines.");
            case ProjectLineKind.Item when ProductId is null:
                throw new ArgumentException("ProductId is required for item lines.");
            case ProjectLineKind.Item when decimal.Truncate(Quantity) != Quantity:
                throw new ArgumentException("Item quantities must be whole numbers.");
        }

        if (Quantity < QuantityTransferredToInvoice)
            throw new ArgumentException(
                $"Quantity cannot be lower than the quantity already transferred to invoice ({QuantityTransferredToInvoice}).");
        if (Quantity < QuantityPosted + QuantityInConsumption && Kind == ProjectLineKind.Item)
            throw new ArgumentException(
                $"Quantity cannot be lower than the quantity already consumed or in consumption ({QuantityPosted + QuantityInConsumption}).");
    }

    /// <summary>Recalculates cost and amounts from quantity, unit cost/price, discount and exchange rate.</summary>
    public void Recalculate()
    {
        if (Kind == ProjectLineKind.Text)
        {
            Quantity = 0;
            UnitCostLcy = 0;
            UnitPrice = 0;
            LineDiscountPercent = 0;
            TotalCostLcy = 0;
            LineAmount = 0;
            LineAmountLcy = 0;
            return;
        }

        TotalCostLcy = Math.Round(Quantity * UnitCostLcy, 2, MidpointRounding.AwayFromZero);
        var gross = Quantity * UnitPrice;
        LineAmount = Math.Round(gross - gross * LineDiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        LineAmountLcy = Math.Round(LineAmount * ExchangeRate, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Unit price after line discount, in project currency.</summary>
    public decimal NetUnitPrice => Math.Round(UnitPrice * (1 - LineDiscountPercent / 100m), 4, MidpointRounding.AwayFromZero);

    /// <summary>Registers posted usage.</summary>
    /// <param name="quantity">The posted quantity (may be negative for corrections).</param>
    public void RegisterUsage(decimal quantity) => QuantityPosted += quantity;

    /// <summary>Moves a quantity into pending consumption (order created in Orders).</summary>
    /// <param name="quantity">The quantity.</param>
    /// <exception cref="InvalidOperationException">When the quantity exceeds the remaining quantity.</exception>
    public void ReserveForConsumption(decimal quantity)
    {
        if (Kind != ProjectLineKind.Item)
            throw new InvalidOperationException("Only item lines can be consumed through Orders.");
        if (quantity <= 0 || quantity > RemainingQuantity)
            throw new InvalidOperationException($"Consumption quantity must be between 1 and {RemainingQuantity}.");

        QuantityInConsumption += quantity;
    }

    /// <summary>Confirms a pending consumption as usage (order fulfilled).</summary>
    /// <param name="quantity">The quantity.</param>
    public void ConfirmConsumption(decimal quantity)
    {
        QuantityInConsumption = Math.Max(0, QuantityInConsumption - quantity);
        QuantityPosted += quantity;
    }

    /// <summary>Releases a pending consumption (order cancelled).</summary>
    /// <param name="quantity">The quantity.</param>
    public void ReleaseConsumption(decimal quantity) =>
        QuantityInConsumption = Math.Max(0, QuantityInConsumption - quantity);

    /// <summary>Marks a quantity as transferred to an invoice request.</summary>
    /// <param name="quantity">The quantity.</param>
    /// <exception cref="InvalidOperationException">When the quantity exceeds the quantity to invoice.</exception>
    public void TransferToInvoice(decimal quantity)
    {
        if (quantity <= 0 || quantity > QuantityToInvoice)
            throw new InvalidOperationException($"Quantity to invoice must be greater than 0 and at most {QuantityToInvoice}.");

        QuantityTransferredToInvoice += quantity;
    }

    /// <summary>Reverts a transferred quantity (request cancelled before or after issuing).</summary>
    /// <param name="quantity">The quantity.</param>
    public void ReverseTransferToInvoice(decimal quantity) =>
        QuantityTransferredToInvoice = Math.Max(0, QuantityTransferredToInvoice - quantity);

    /// <summary>Registers an invoiced quantity (negative to reverse).</summary>
    /// <param name="quantity">The quantity.</param>
    public void RegisterInvoiced(decimal quantity) =>
        QuantityInvoiced = Math.Max(0, QuantityInvoiced + quantity);
}
