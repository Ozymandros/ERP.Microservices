using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>
/// Project aggregate root (Business Central "Project"/"Job").
/// Commercial + project-control context: holds the bill-to customer and the project currency,
/// never physical stock (Orders) nor invoices (Billing).
/// </summary>
public class Project(Guid id) : AuditableEntity<Guid>(id)
{
    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> AllowedTransitions = new()
    {
        [ProjectStatus.Planning] = [ProjectStatus.Quote, ProjectStatus.Open],
        [ProjectStatus.Quote] = [ProjectStatus.Planning, ProjectStatus.Open],
        [ProjectStatus.Open] = [ProjectStatus.Completed],
        [ProjectStatus.Completed] = [ProjectStatus.Open]
    };

    /// <summary>Gets or sets the project number (e.g. PRJ-2026-00001).</summary>
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the bill-to customer id (owned by the Sales service).</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the project manager user id (owned by the Auth service).</summary>
    public Guid? ProjectManagerUserId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    /// <summary>Gets or sets the blocking level.</summary>
    public ProjectBlocked Blocked { get; set; } = ProjectBlocked.None;

    /// <summary>Gets or sets the starting date.</summary>
    public DateTime? StartingDate { get; set; }

    /// <summary>Gets or sets the ending date.</summary>
    public DateTime? EndingDate { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency in which the project is priced and invoiced.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional external reference (customer PO, contract no., ...).</summary>
    public string? ExternalReference { get; set; }

    /// <summary>Gets or sets the tasks of the project.</summary>
    public List<ProjectTask> Tasks { get; set; } = new();

    /// <summary>Returns <see langword="true"/> when the transition to <paramref name="newStatus"/> is allowed.</summary>
    /// <param name="newStatus">The target status.</param>
    public bool CanTransitionTo(ProjectStatus newStatus) =>
        newStatus == Status || AllowedTransitions[Status].Contains(newStatus);

    /// <summary>Changes the status, enforcing the allowed transitions.</summary>
    /// <param name="newStatus">The target status.</param>
    /// <exception cref="InvalidOperationException">When the transition is not allowed.</exception>
    public void ChangeStatus(ProjectStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Project {ProjectNumber} cannot change status from {Status} to {newStatus}.");

        Status = newStatus;
    }

    /// <summary>Ensures the project (header, tasks, planning lines) can be modified.</summary>
    /// <exception cref="InvalidOperationException">When the project is completed or fully blocked.</exception>
    public void EnsureCanModify()
    {
        if (Status == ProjectStatus.Completed)
            throw new InvalidOperationException($"Project {ProjectNumber} is completed and cannot be modified.");

        if (Blocked == ProjectBlocked.All)
            throw new InvalidOperationException($"Project {ProjectNumber} is blocked.");
    }

    /// <summary>Ensures usage, consumption and invoicing can be posted.</summary>
    /// <exception cref="InvalidOperationException">When the project is not Open or posting is blocked.</exception>
    public void EnsureCanPost()
    {
        if (Status != ProjectStatus.Open)
            throw new InvalidOperationException($"Project {ProjectNumber} must be Open to post (current status: {Status}).");

        if (Blocked != ProjectBlocked.None)
            throw new InvalidOperationException($"Project {ProjectNumber} is blocked for posting ({Blocked}).");
    }

    /// <summary>Validates the date range.</summary>
    /// <exception cref="ArgumentException">When the ending date is before the starting date.</exception>
    public void ValidateDates()
    {
        if (StartingDate.HasValue && EndingDate.HasValue && EndingDate.Value.Date < StartingDate.Value.Date)
            throw new ArgumentException("EndingDate cannot be earlier than StartingDate.");
    }
}
