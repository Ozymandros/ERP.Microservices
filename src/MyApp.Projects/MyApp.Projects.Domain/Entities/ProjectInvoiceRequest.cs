using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>
/// Invoice request sent to Billing through pub/sub. Its <c>Id</c> is the correlation id that Billing
/// stores as the invoice SourceId (SourceType = "Project").
/// </summary>
public class ProjectInvoiceRequest(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the project id.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the bill-to customer id at request time.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the currency of the request.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the status.</summary>
    public ProjectInvoiceRequestStatus Status { get; set; } = ProjectInvoiceRequestStatus.Requested;

    /// <summary>Gets or sets the request date.</summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>Gets or sets the Billing invoice id once known.</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>Gets or sets the Billing invoice number once issued.</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>Gets or sets the total amount (project currency, excl. tax).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the lines.</summary>
    public List<ProjectInvoiceRequestLine> Lines { get; set; } = new();
}

/// <summary>A line of a <see cref="ProjectInvoiceRequest"/>.</summary>
public class ProjectInvoiceRequestLine(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the request id.</summary>
    public Guid InvoiceRequestId { get; set; }

    /// <summary>Gets or sets the planning line id.</summary>
    public Guid PlanningLineId { get; set; }

    /// <summary>Gets or sets the task id.</summary>
    public Guid ProjectTaskId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the net unit price (after line discount) in project currency.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the tax rate percentage sent to Billing.</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Gets or sets the line amount (project currency).</summary>
    public decimal LineAmount { get; set; }
}
