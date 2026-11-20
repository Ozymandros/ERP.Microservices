using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>
/// Project task organised as a parent/child tree (<see cref="ParentTaskId"/>).
/// Only <see cref="ProjectTaskType.Posting"/> tasks without children accept planning lines and postings;
/// amounts of <see cref="ProjectTaskType.Heading"/> tasks are rolled up from their descendants.
/// </summary>
public class ProjectTask(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the owning project id.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the parent task id (null for root tasks).</summary>
    public Guid? ParentTaskId { get; set; }

    /// <summary>Gets or sets the task number, unique within the project (e.g. "1000", "1010").</summary>
    public string TaskNo { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the task type.</summary>
    public ProjectTaskType TaskType { get; set; } = ProjectTaskType.Posting;

    /// <summary>Gets or sets the order among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the planned start date.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Gets or sets the planned end date.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Gets or sets the project navigation.</summary>
    public Project? Project { get; set; }

    /// <summary>Gets or sets the parent navigation.</summary>
    public ProjectTask? ParentTask { get; set; }

    /// <summary>Gets or sets the children navigation.</summary>
    public List<ProjectTask> Children { get; set; } = new();
}
