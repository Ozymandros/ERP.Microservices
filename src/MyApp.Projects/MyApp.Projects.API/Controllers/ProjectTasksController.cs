using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Projects.API.Controllers;

/// <summary>Project task tree (parent/child).</summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/tasks")]
[Produces("application/json")]
public class ProjectTasksController : ProjectsApiControllerBase
{
    private readonly IProjectTaskService _taskService;

    /// <summary>Initializes a new instance of the <see cref="ProjectTasksController"/> class.</summary>
    public ProjectTasksController(IProjectTaskService taskService, ICacheService cacheService, ILogger<ProjectTasksController> logger)
        : base(cacheService, logger)
    {
        _taskService = taskService;
    }

    /// <summary>Export the tasks of a project as XLSX.</summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ExportToXlsx(Guid projectId) => ExecuteAsync(async () =>
    {
        var tasks = await _taskService.ListAsync(projectId, HttpContext.RequestAborted);
        return File(tasks.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProjectTasks.xlsx");
    }, "exporting project tasks");

    /// <summary>Export the tasks of a project as PDF.</summary>
    [HttpGet("export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportToPdf(Guid projectId) => ExecuteAsync(async () =>
    {
        var tasks = await _taskService.ListAsync(projectId, HttpContext.RequestAborted);
        return File(tasks.ExportToPdf(), "application/pdf", "ProjectTasks.pdf");
    }, "exporting project tasks");

    /// <summary>List the tasks of a project (flat, depth-first order).</summary>
    [HttpGet]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<ProjectTaskDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(Guid projectId) => ExecuteAsync(
        async () => Ok(await _taskService.ListAsync(projectId, HttpContext.RequestAborted)),
        "retrieving project tasks");

    /// <summary>Get the task tree of a project.</summary>
    [HttpGet("tree")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<ProjectTaskTreeNodeDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetTree(Guid projectId) => ExecuteAsync(
        async () => Ok(await _taskService.GetTreeAsync(projectId, HttpContext.RequestAborted)),
        "retrieving the task tree");

    /// <summary>Get a task.</summary>
    [HttpGet("{taskId:guid}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(ProjectTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid projectId, Guid taskId) => ExecuteAsync(async () =>
    {
        var task = await _taskService.GetByIdAsync(projectId, taskId, HttpContext.RequestAborted);
        return task is null ? NotFound(new { message = $"Task {taskId} not found." }) : Ok(task);
    }, "retrieving the task");

    /// <summary>Create a task (optionally under a parent; a Posting parent without data becomes a Heading).</summary>
    [HttpPost]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(ProjectTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create(Guid projectId, [FromBody] CreateProjectTaskDto dto) => ExecuteAsync(async () =>
    {
        var task = await _taskService.CreateAsync(projectId, dto, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { projectId, taskId = task.Id }, task);
    }, "creating the task");

    /// <summary>Update a task.</summary>
    [HttpPut("{taskId:guid}")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(ProjectTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid projectId, Guid taskId, [FromBody] UpdateProjectTaskDto dto) => ExecuteAsync(
        async () => Ok(await _taskService.UpdateAsync(projectId, taskId, dto, HttpContext.RequestAborted)),
        "updating the task");

    /// <summary>Move a task to another parent (or root) and/or position.</summary>
    [HttpPost("{taskId:guid}/move")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(ProjectTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Move(Guid projectId, Guid taskId, [FromBody] MoveProjectTaskDto dto) => ExecuteAsync(
        async () => Ok(await _taskService.MoveAsync(projectId, taskId, dto, HttpContext.RequestAborted)),
        "moving the task");

    /// <summary>Delete a task (no children, planning lines or ledger entries).</summary>
    [HttpDelete("{taskId:guid}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid projectId, Guid taskId) => ExecuteAsync(async () =>
    {
        await _taskService.DeleteAsync(projectId, taskId, HttpContext.RequestAborted);
        return NoContent();
    }, "deleting the task");
}
