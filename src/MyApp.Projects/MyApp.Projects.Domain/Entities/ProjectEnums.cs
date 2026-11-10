namespace MyApp.Projects.Domain.Entities;

/// <summary>Lifecycle status of a project (mirrors Business Central project statuses).</summary>
public enum ProjectStatus
{
    /// <summary>The project is being planned; no posting allowed.</summary>
    Planning = 0,
    /// <summary>The project is quoted to the customer; no posting allowed.</summary>
    Quote = 1,
    /// <summary>The project is running; usage and invoicing can be posted.</summary>
    Open = 2,
    /// <summary>The project is finished; no further changes allowed (can be reopened).</summary>
    Completed = 3
}

/// <summary>Blocking level of a project.</summary>
public enum ProjectBlocked
{
    /// <summary>Not blocked.</summary>
    None = 0,
    /// <summary>Posting (usage, consumption, invoicing) is blocked; planning is allowed.</summary>
    Posting = 1,
    /// <summary>Everything is blocked.</summary>
    All = 2
}

/// <summary>Type of a project task.</summary>
public enum ProjectTaskType
{
    /// <summary>A leaf task that accepts planning lines and postings.</summary>
    Posting = 0,
    /// <summary>A grouping task; amounts roll up from its children.</summary>
    Heading = 1
}

/// <summary>Planning line type (budget vs. billable).</summary>
public enum PlanningLineType
{
    /// <summary>Budget only (cost planning).</summary>
    Budget = 0,
    /// <summary>Billable only (to be invoiced to the customer).</summary>
    Billable = 1,
    /// <summary>Both budget and billable.</summary>
    BudgetAndBillable = 2
}

/// <summary>What a planning line / ledger entry refers to.</summary>
public enum ProjectLineKind
{
    /// <summary>A project resource (person or machine).</summary>
    Resource = 0,
    /// <summary>An inventory item (consumed through Orders).</summary>
    Item = 1,
    /// <summary>A free expense (travel, subcontracting, ...).</summary>
    Expense = 2,
    /// <summary>A text-only line, no amounts.</summary>
    Text = 3
}

/// <summary>Project ledger entry type.</summary>
public enum ProjectLedgerEntryType
{
    /// <summary>Usage (cost) posted to the project.</summary>
    Usage = 0,
    /// <summary>Sale (invoiced) posted to the project.</summary>
    Sale = 1
}

/// <summary>Origin of a project ledger entry.</summary>
public enum ProjectSourceDocument
{
    /// <summary>Manual usage posting.</summary>
    Manual = 0,
    /// <summary>Item consumption fulfilled by an operational order.</summary>
    ConsumptionOrder = 1,
    /// <summary>Customer invoice issued by Billing.</summary>
    Invoice = 2
}

/// <summary>Type of a resource.</summary>
public enum ResourceType
{
    /// <summary>A person.</summary>
    Person = 0,
    /// <summary>A machine.</summary>
    Machine = 1
}

/// <summary>Status of a project invoice request sent to Billing.</summary>
public enum ProjectInvoiceRequestStatus
{
    /// <summary>Published to Billing, no invoice yet.</summary>
    Requested = 0,
    /// <summary>Billing created a draft invoice.</summary>
    Drafted = 1,
    /// <summary>Billing issued the invoice; sale entries were posted.</summary>
    Invoiced = 2,
    /// <summary>The invoice was cancelled; quantities were released.</summary>
    Cancelled = 3
}

/// <summary>Status of an item consumption order sent to Orders.</summary>
public enum ProjectConsumptionStatus
{
    /// <summary>Order created in Orders, waiting for fulfilment.</summary>
    Pending = 0,
    /// <summary>Order fulfilled; usage entries were posted.</summary>
    Fulfilled = 1,
    /// <summary>Order cancelled; nothing was posted.</summary>
    Cancelled = 2
}
