using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>
/// Tracks an Outbound operational order created in the Orders service to consume items for a project
/// (ExternalOrderId = project id, ExternalOrderSource = "Project").
/// </summary>
public class ProjectConsumptionOrder(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the project id.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the operational order id in the Orders service.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the operational order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the warehouse id.</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public ProjectConsumptionStatus Status { get; set; } = ProjectConsumptionStatus.Pending;

    /// <summary>Gets or sets the requested posting date for usage entries.</summary>
    public DateTime PostingDate { get; set; }

    /// <summary>Gets or sets the lines.</summary>
    public List<ProjectConsumptionOrderLine> Lines { get; set; } = new();
}

/// <summary>A line of a <see cref="ProjectConsumptionOrder"/>.</summary>
public class ProjectConsumptionOrderLine(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the consumption order id.</summary>
    public Guid ConsumptionOrderId { get; set; }

    /// <summary>Gets or sets the planning line id.</summary>
    public Guid PlanningLineId { get; set; }

    /// <summary>Gets or sets the product id.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the quantity.</summary>
    public int Quantity { get; set; }
}
