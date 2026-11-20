using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Projects.Domain.Services;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Projects.Application.Services;

/// <summary>
/// Task tree operations. Rules:
/// <list type="bullet">
/// <item>TaskNo is unique within the project.</item>
/// <item>A task cannot become its own ancestor (no cycles).</item>
/// <item>Only Heading tasks can have children; a Posting task with planning lines or ledger entries cannot become a parent.</item>
/// <item>Only leaf Posting tasks accept planning lines and postings (enforced by planning/posting services).</item>
/// </list>
/// </summary>
public class ProjectTaskService : AppServiceBase, IProjectTaskService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectTaskRepository _tasks;
    private readonly IProjectPlanningLineRepository _planningLines;
    private readonly IProjectLedgerEntryRepository _ledger;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="ProjectTaskService"/> class.</summary>
    public ProjectTaskService(
        IProjectRepository projects,
        IProjectTaskRepository tasks,
        IProjectPlanningLineRepository planningLines,
        IProjectLedgerEntryRepository ledger,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ProjectTaskService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Projects)
    {
        _projects = projects;
        _tasks = tasks;
        _planningLines = planningLines;
        _ledger = ledger;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<List<ProjectTaskDto>> ListAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await GetProjectAsync(projectId);
        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        return OrderDepthFirst(tasks).Select(x => _mapper.Map<ProjectTaskDto>(x.Task)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ProjectTaskTreeNodeDto>> GetTreeAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await GetProjectAsync(projectId);
        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        var byParent = tasks.ToLookup(t => t.ParentTaskId);

        List<ProjectTaskTreeNodeDto> Build(Guid? parentId, int level) =>
            byParent[parentId]
                .OrderBy(t => t.SortOrder).ThenBy(t => t.TaskNo, StringComparer.Ordinal)
                .Select(t => new ProjectTaskTreeNodeDto
                {
                    Task = _mapper.Map<ProjectTaskDto>(t),
                    Level = level,
                    Children = Build(t.Id, level + 1)
                })
                .ToList();

        return Build(null, 0);
    }

    /// <inheritdoc />
    public async Task<ProjectTaskDto?> GetByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        return task is null || task.ProjectId != projectId ? null : _mapper.Map<ProjectTaskDto>(task);
    }

    /// <inheritdoc />
    public async Task<ProjectTaskDto> CreateAsync(Guid projectId, CreateProjectTaskDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();

        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        var taskNo = dto.TaskNo.Trim();
        EnsureUniqueTaskNo(tasks, taskNo, null);
        ValidateDates(dto.StartDate, dto.EndDate);

        if (dto.ParentTaskId.HasValue)
            await EnsureCanBeParentAsync(tasks, dto.ParentTaskId.Value, cancellationToken);

        var task = new ProjectTask(Guid.NewGuid())
        {
            ProjectId = projectId,
            ParentTaskId = dto.ParentTaskId,
            TaskNo = taskNo,
            Description = dto.Description.Trim(),
            TaskType = dto.TaskType,
            SortOrder = dto.SortOrder ?? NextSortOrder(tasks, dto.ParentTaskId),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        await _tasks.AddAsync(task);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectTaskDto>(task);
    }

    /// <inheritdoc />
    public async Task<ProjectTaskDto> UpdateAsync(Guid projectId, Guid taskId, UpdateProjectTaskDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();

        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        var task = tasks.FirstOrDefault(t => t.Id == taskId) ?? throw new NotFoundException(nameof(ProjectTask), taskId);

        var taskNo = dto.TaskNo.Trim();
        EnsureUniqueTaskNo(tasks, taskNo, task.Id);
        ValidateDates(dto.StartDate, dto.EndDate);

        if (dto.TaskType != task.TaskType)
        {
            if (dto.TaskType == ProjectTaskType.Posting && ProjectTaskTree.HasChildren(tasks, task.Id))
                throw new InvalidOperationException("A task with children must be a Heading task.");
            if (dto.TaskType == ProjectTaskType.Heading && await HasPostingDataAsync(task.Id, cancellationToken))
                throw new InvalidOperationException("A task with planning lines or ledger entries must remain a Posting task.");
        }

        task.TaskNo = taskNo;
        task.Description = dto.Description.Trim();
        task.TaskType = dto.TaskType;
        task.StartDate = dto.StartDate;
        task.EndDate = dto.EndDate;

        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectTaskDto>(task);
    }

    /// <inheritdoc />
    public async Task<ProjectTaskDto> MoveAsync(Guid projectId, Guid taskId, MoveProjectTaskDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();

        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        var task = tasks.FirstOrDefault(t => t.Id == taskId) ?? throw new NotFoundException(nameof(ProjectTask), taskId);

        if (ProjectTaskTree.WouldCreateCycle(tasks, task.Id, dto.NewParentTaskId))
            throw new InvalidOperationException("A task cannot be moved under itself or one of its descendants.");

        if (dto.NewParentTaskId.HasValue && dto.NewParentTaskId != task.ParentTaskId)
            await EnsureCanBeParentAsync(tasks, dto.NewParentTaskId.Value, cancellationToken);

        var parentChanged = task.ParentTaskId != dto.NewParentTaskId;
        task.ParentTaskId = dto.NewParentTaskId;
        task.SortOrder = dto.SortOrder ?? (parentChanged ? NextSortOrder(tasks.Where(t => t.Id != task.Id), dto.NewParentTaskId) : task.SortOrder);

        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectTaskDto>(task);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();

        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        var task = tasks.FirstOrDefault(t => t.Id == taskId) ?? throw new NotFoundException(nameof(ProjectTask), taskId);

        if (ProjectTaskTree.HasChildren(tasks, task.Id))
            throw new InvalidOperationException("Delete or move the child tasks first.");
        if (await HasPostingDataAsync(task.Id, cancellationToken))
            throw new InvalidOperationException("Tasks with planning lines or ledger entries cannot be deleted.");

        await _tasks.DeleteAsync(task);
        await SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetProjectAsync(Guid projectId) =>
        await _projects.GetByIdAsync(projectId) ?? throw new NotFoundException(nameof(Project), projectId);

    private async Task EnsureCanBeParentAsync(IReadOnlyCollection<ProjectTask> tasks, Guid parentId, CancellationToken cancellationToken)
    {
        var parent = tasks.FirstOrDefault(t => t.Id == parentId)
            ?? throw new ArgumentException($"Parent task {parentId} does not belong to this project.");

        if (parent.TaskType != ProjectTaskType.Heading)
        {
            if (await HasPostingDataAsync(parent.Id, cancellationToken))
                throw new InvalidOperationException(
                    $"Task {parent.TaskNo} has planning lines or ledger entries and cannot get child tasks.");

            // A Posting task without data is promoted to Heading when it receives children.
            parent.TaskType = ProjectTaskType.Heading;
        }
    }

    private async Task<bool> HasPostingDataAsync(Guid taskId, CancellationToken cancellationToken) =>
        await _planningLines.AnyForTaskAsync(taskId, cancellationToken)
        || await _ledger.AnyForTaskAsync(taskId, cancellationToken);

    private static void EnsureUniqueTaskNo(IEnumerable<ProjectTask> tasks, string taskNo, Guid? exceptId)
    {
        if (tasks.Any(t => t.Id != exceptId && string.Equals(t.TaskNo, taskNo, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Task number {taskNo} already exists in this project.");
    }

    private static void ValidateDates(DateTime? start, DateTime? end)
    {
        if (start.HasValue && end.HasValue && end.Value.Date < start.Value.Date)
            throw new ArgumentException("EndDate cannot be earlier than StartDate.");
    }

    private static int NextSortOrder(IEnumerable<ProjectTask> tasks, Guid? parentId) =>
        tasks.Where(t => t.ParentTaskId == parentId).Select(t => t.SortOrder).DefaultIfEmpty(0).Max() + 10;

    private static IEnumerable<(ProjectTask Task, int Level)> OrderDepthFirst(IReadOnlyCollection<ProjectTask> tasks)
    {
        var byParent = tasks.ToLookup(t => t.ParentTaskId);

        IEnumerable<(ProjectTask, int)> Walk(Guid? parentId, int level)
        {
            foreach (var task in byParent[parentId].OrderBy(t => t.SortOrder).ThenBy(t => t.TaskNo, StringComparer.Ordinal))
            {
                yield return (task, level);
                foreach (var child in Walk(task.Id, level + 1))
                    yield return child;
            }
        }

        return Walk(null, 0);
    }
}
