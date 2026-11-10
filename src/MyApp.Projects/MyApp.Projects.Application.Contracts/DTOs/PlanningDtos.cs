using System.ComponentModel.DataAnnotations;
using MyApp.Projects.Domain.Entities;
using MyApp.Shared.Domain.DTOs;

namespace MyApp.Projects.Application.Contracts.DTOs;

/// <summary>Planning line read model.</summary>
public record PlanningLineDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the project id.</summary>
    public Guid ProjectId { get; init; }
    /// <summary>Gets the task id.</summary>
    public Guid ProjectTaskId { get; init; }
    /// <summary>Gets the line number.</summary>
    public int LineNo { get; init; }
    /// <summary>Gets the line type.</summary>
    public string LineType { get; init; } = string.Empty;
    /// <summary>Gets the kind.</summary>
    public string Kind { get; init; } = string.Empty;
    /// <summary>Gets the resource id.</summary>
    public Guid? ResourceId { get; init; }
    /// <summary>Gets the product id.</summary>
    public Guid? ProductId { get; init; }
    /// <summary>Gets the warehouse id.</summary>
    public Guid? WarehouseId { get; init; }
    /// <summary>Gets the description.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the unit of measure.</summary>
    public string? UnitOfMeasure { get; init; }
    /// <summary>Gets the planning date.</summary>
    public DateTime PlanningDate { get; init; }
    /// <summary>Gets the quantity.</summary>
    public decimal Quantity { get; init; }
    /// <summary>Gets the unit cost (LCY).</summary>
    public decimal UnitCostLcy { get; init; }
    /// <summary>Gets the unit price (project currency).</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Gets the line discount %.</summary>
    public decimal LineDiscountPercent { get; init; }
    /// <summary>Gets the project currency.</summary>
    public string CurrencyCode { get; init; } = string.Empty;
    /// <summary>Gets the exchange rate (LCY per unit).</summary>
    public decimal ExchangeRate { get; init; }
    /// <summary>Gets the total cost (LCY).</summary>
    public decimal TotalCostLcy { get; init; }
    /// <summary>Gets the line amount (project currency).</summary>
    public decimal LineAmount { get; init; }
    /// <summary>Gets the line amount (LCY).</summary>
    public decimal LineAmountLcy { get; init; }
    /// <summary>Gets the posted usage quantity.</summary>
    public decimal QuantityPosted { get; init; }
    /// <summary>Gets the quantity pending in consumption orders.</summary>
    public decimal QuantityInConsumption { get; init; }
    /// <summary>Gets the quantity transferred to invoice requests.</summary>
    public decimal QuantityTransferredToInvoice { get; init; }
    /// <summary>Gets the invoiced quantity.</summary>
    public decimal QuantityInvoiced { get; init; }
    /// <summary>Gets the quantity still available to invoice.</summary>
    public decimal QuantityToInvoice { get; init; }
    /// <summary>Gets the remaining quantity to use.</summary>
    public decimal RemainingQuantity { get; init; }
}

/// <summary>
/// Payload to create or update a planning line. When unit cost/price are omitted they default from the
/// resource (LCY) or product (Inventory unit price, LCY) and prices are converted to the project currency.
/// </summary>
public record CreateUpdatePlanningLineDto
{
    /// <summary>Gets the task id (leaf posting task).</summary>
    [Required]
    public Guid ProjectTaskId { get; init; }
    /// <summary>Gets the line type.</summary>
    public PlanningLineType LineType { get; init; } = PlanningLineType.BudgetAndBillable;
    /// <summary>Gets the kind.</summary>
    public ProjectLineKind Kind { get; init; } = ProjectLineKind.Resource;
    /// <summary>Gets the resource id (Kind = Resource).</summary>
    public Guid? ResourceId { get; init; }
    /// <summary>Gets the product id (Kind = Item).</summary>
    public Guid? ProductId { get; init; }
    /// <summary>Gets the warehouse id (Kind = Item).</summary>
    public Guid? WarehouseId { get; init; }
    /// <summary>Gets the description (defaults from resource/product).</summary>
    [StringLength(250)]
    public string? Description { get; init; }
    /// <summary>Gets the unit of measure.</summary>
    [StringLength(20)]
    public string? UnitOfMeasure { get; init; }
    /// <summary>Gets the planning date (defaults to today).</summary>
    public DateTime? PlanningDate { get; init; }
    /// <summary>Gets the quantity.</summary>
    [Range(0, 999999999)]
    public decimal Quantity { get; init; }
    /// <summary>Gets the unit cost in LCY.</summary>
    [Range(0, 999999999)]
    public decimal? UnitCostLcy { get; init; }
    /// <summary>Gets the unit price in project currency.</summary>
    [Range(0, 999999999)]
    public decimal? UnitPrice { get; init; }
    /// <summary>Gets the line discount %.</summary>
    [Range(0, 100)]
    public decimal LineDiscountPercent { get; init; }
}

/// <summary>Project ledger entry read model.</summary>
public record ProjectLedgerEntryDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the entry number.</summary>
    public int EntryNo { get; init; }
    /// <summary>Gets the entry type.</summary>
    public string EntryType { get; init; } = string.Empty;
    /// <summary>Gets the posting date.</summary>
    public DateTime PostingDate { get; init; }
    /// <summary>Gets the project id.</summary>
    public Guid ProjectId { get; init; }
    /// <summary>Gets the task id.</summary>
    public Guid ProjectTaskId { get; init; }
    /// <summary>Gets the planning line id.</summary>
    public Guid? PlanningLineId { get; init; }
    /// <summary>Gets the kind.</summary>
    public string Kind { get; init; } = string.Empty;
    /// <summary>Gets the resource id.</summary>
    public Guid? ResourceId { get; init; }
    /// <summary>Gets the product id.</summary>
    public Guid? ProductId { get; init; }
    /// <summary>Gets the description.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the quantity.</summary>
    public decimal Quantity { get; init; }
    /// <summary>Gets the unit cost (LCY).</summary>
    public decimal UnitCostLcy { get; init; }
    /// <summary>Gets the total cost (LCY).</summary>
    public decimal TotalCostLcy { get; init; }
    /// <summary>Gets the unit price (project currency).</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Gets the line amount (project currency).</summary>
    public decimal LineAmount { get; init; }
    /// <summary>Gets the line amount (LCY).</summary>
    public decimal LineAmountLcy { get; init; }
    /// <summary>Gets the currency.</summary>
    public string CurrencyCode { get; init; } = string.Empty;
    /// <summary>Gets the exchange rate.</summary>
    public decimal ExchangeRate { get; init; }
    /// <summary>Gets the source document type.</summary>
    public string SourceDocument { get; init; } = string.Empty;
    /// <summary>Gets the source document id.</summary>
    public Guid? SourceDocumentId { get; init; }
    /// <summary>Gets the source document number.</summary>
    public string? SourceDocumentNo { get; init; }
}

/// <summary>
/// Payload to post resource/expense usage (Business Central project journal, simplified).
/// Items are consumed through <see cref="CreateItemConsumptionDto"/> instead.
/// </summary>
public record PostUsageDto
{
    /// <summary>Gets the task id (leaf posting task).</summary>
    [Required]
    public Guid ProjectTaskId { get; init; }
    /// <summary>Gets the planning line to link the usage to (optional).</summary>
    public Guid? PlanningLineId { get; init; }
    /// <summary>Gets the kind (Resource or Expense).</summary>
    public ProjectLineKind Kind { get; init; } = ProjectLineKind.Resource;
    /// <summary>Gets the resource id (Kind = Resource).</summary>
    public Guid? ResourceId { get; init; }
    /// <summary>Gets the description.</summary>
    [StringLength(250)]
    public string? Description { get; init; }
    /// <summary>Gets the quantity (negative to correct a previous posting).</summary>
    public decimal Quantity { get; init; }
    /// <summary>Gets the unit cost in LCY (defaults from planning line or resource).</summary>
    [Range(0, 999999999)]
    public decimal? UnitCostLcy { get; init; }
    /// <summary>Gets the unit price in project currency (defaults from planning line or resource).</summary>
    [Range(0, 999999999)]
    public decimal? UnitPrice { get; init; }
    /// <summary>Gets the posting date (defaults to today).</summary>
    public DateTime? PostingDate { get; init; }
}

/// <summary>Payload to consume items through an Outbound order in the Orders service.</summary>
public record CreateItemConsumptionDto
{
    /// <summary>Gets the warehouse to ship from.</summary>
    [Required]
    public Guid WarehouseId { get; init; }
    /// <summary>Gets the posting date for the resulting usage entries (defaults to fulfilment date).</summary>
    public DateTime? PostingDate { get; init; }
    /// <summary>Gets the delivery address (job site), optional.</summary>
    [StringLength(500)]
    public string? DestinationAddress { get; init; }
    /// <summary>Gets the lines to consume.</summary>
    [Required, MinLength(1)]
    public List<ItemConsumptionLineDto> Lines { get; init; } = new();
}

/// <summary>A line to consume.</summary>
public record ItemConsumptionLineDto
{
    /// <summary>Gets the item planning line id.</summary>
    [Required]
    public Guid PlanningLineId { get; init; }
    /// <summary>Gets the quantity (whole units).</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}

/// <summary>Consumption order read model.</summary>
public record ConsumptionOrderDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the project id.</summary>
    public Guid ProjectId { get; init; }
    /// <summary>Gets the Orders-service order id.</summary>
    public Guid OrderId { get; init; }
    /// <summary>Gets the Orders-service order number.</summary>
    public string OrderNumber { get; init; } = string.Empty;
    /// <summary>Gets the warehouse id.</summary>
    public Guid WarehouseId { get; init; }
    /// <summary>Gets the status.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Gets the posting date.</summary>
    public DateTime PostingDate { get; init; }
    /// <summary>Gets the lines.</summary>
    public List<ConsumptionOrderLineDto> Lines { get; init; } = new();
}

/// <summary>Consumption order line read model.</summary>
public record ConsumptionOrderLineDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the planning line id.</summary>
    public Guid PlanningLineId { get; init; }
    /// <summary>Gets the product id.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets the quantity.</summary>
    public int Quantity { get; init; }
}

/// <summary>Payload to request a customer invoice from billable planning lines.</summary>
public record CreateInvoiceRequestDto
{
    /// <summary>
    /// Gets the lines to invoice. When empty, all billable lines with a remaining quantity to invoice are used
    /// with their full remaining quantity.
    /// </summary>
    public List<InvoiceRequestLineInputDto> Lines { get; init; } = new();
    /// <summary>Gets the tax rate percentage applied to every line (e.g. 21).</summary>
    [Range(0, 100)]
    public decimal TaxRate { get; init; }
}

/// <summary>A line to invoice.</summary>
public record InvoiceRequestLineInputDto
{
    /// <summary>Gets the billable planning line id.</summary>
    [Required]
    public Guid PlanningLineId { get; init; }
    /// <summary>Gets the quantity (defaults to the remaining quantity to invoice).</summary>
    [Range(0, 999999999)]
    public decimal? Quantity { get; init; }
}

/// <summary>Invoice request read model.</summary>
public record InvoiceRequestDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the project id.</summary>
    public Guid ProjectId { get; init; }
    /// <summary>Gets the customer id.</summary>
    public Guid CustomerId { get; init; }
    /// <summary>Gets the currency.</summary>
    public string CurrencyCode { get; init; } = string.Empty;
    /// <summary>Gets the status.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Gets the request date.</summary>
    public DateTime RequestedAt { get; init; }
    /// <summary>Gets the Billing invoice id.</summary>
    public Guid? InvoiceId { get; init; }
    /// <summary>Gets the Billing invoice number.</summary>
    public string? InvoiceNumber { get; init; }
    /// <summary>Gets the total amount (excl. tax).</summary>
    public decimal TotalAmount { get; init; }
    /// <summary>Gets the lines.</summary>
    public List<InvoiceRequestLineDto> Lines { get; init; } = new();
}

/// <summary>Invoice request line read model.</summary>
public record InvoiceRequestLineDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>Gets the planning line id.</summary>
    public Guid PlanningLineId { get; init; }
    /// <summary>Gets the task id.</summary>
    public Guid ProjectTaskId { get; init; }
    /// <summary>Gets the description.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the quantity.</summary>
    public decimal Quantity { get; init; }
    /// <summary>Gets the net unit price.</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Gets the tax rate.</summary>
    public decimal TaxRate { get; init; }
    /// <summary>Gets the line amount.</summary>
    public decimal LineAmount { get; init; }
}

/// <summary>Amounts for a task or the whole project.</summary>
public record ProjectAmountsDto
{
    /// <summary>Gets the budget cost in LCY (budget lines).</summary>
    public decimal BudgetCostLcy { get; init; }
    /// <summary>Gets the budget price in project currency (budget lines).</summary>
    public decimal BudgetPrice { get; init; }
    /// <summary>Gets the billable price in project currency (billable lines).</summary>
    public decimal BillablePrice { get; init; }
    /// <summary>Gets the usage cost in LCY.</summary>
    public decimal UsageCostLcy { get; init; }
    /// <summary>Gets the usage price in project currency.</summary>
    public decimal UsagePrice { get; init; }
    /// <summary>Gets the invoiced amount in project currency.</summary>
    public decimal InvoicedPrice { get; init; }
    /// <summary>Gets the invoiced amount in LCY.</summary>
    public decimal InvoicedPriceLcy { get; init; }

    /// <summary>Gets the remaining budget cost in LCY.</summary>
    public decimal RemainingBudgetCostLcy => BudgetCostLcy - UsageCostLcy;
    /// <summary>Gets the profit in LCY (invoiced - usage cost).</summary>
    public decimal ProfitLcy => InvoicedPriceLcy - UsageCostLcy;

    /// <summary>Adds two amount sets.</summary>
    public static ProjectAmountsDto operator +(ProjectAmountsDto a, ProjectAmountsDto b) => new()
    {
        BudgetCostLcy = a.BudgetCostLcy + b.BudgetCostLcy,
        BudgetPrice = a.BudgetPrice + b.BudgetPrice,
        BillablePrice = a.BillablePrice + b.BillablePrice,
        UsageCostLcy = a.UsageCostLcy + b.UsageCostLcy,
        UsagePrice = a.UsagePrice + b.UsagePrice,
        InvoicedPrice = a.InvoicedPrice + b.InvoicedPrice,
        InvoicedPriceLcy = a.InvoicedPriceLcy + b.InvoicedPriceLcy
    };
}

/// <summary>Statistics of one task (own amounts rolled up with its descendants).</summary>
public record TaskStatisticsDto
{
    /// <summary>Gets the task id.</summary>
    public Guid TaskId { get; init; }
    /// <summary>Gets the parent task id.</summary>
    public Guid? ParentTaskId { get; init; }
    /// <summary>Gets the task number.</summary>
    public string TaskNo { get; init; } = string.Empty;
    /// <summary>Gets the description.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the task type.</summary>
    public string TaskType { get; init; } = string.Empty;
    /// <summary>Gets the rolled-up amounts.</summary>
    public ProjectAmountsDto Amounts { get; init; } = new();
}

/// <summary>Project statistics (Business Central "Project Statistics", without WIP).</summary>
public record ProjectStatisticsDto
{
    /// <summary>Gets the project id.</summary>
    public Guid ProjectId { get; init; }
    /// <summary>Gets the project currency.</summary>
    public string CurrencyCode { get; init; } = string.Empty;
    /// <summary>Gets the local currency.</summary>
    public string LocalCurrency { get; init; } = string.Empty;
    /// <summary>Gets the project totals.</summary>
    public ProjectAmountsDto Totals { get; init; } = new();
    /// <summary>Gets per-task statistics.</summary>
    public List<TaskStatisticsDto> Tasks { get; init; } = new();
}
