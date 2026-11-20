using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Projects.API.Controllers;

/// <summary>Project planning lines (budget / billable).</summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/planning-lines")]
[Produces("application/json")]
public class ProjectPlanningLinesController : ProjectsApiControllerBase
{
    private readonly IPlanningLineService _planningLineService;

    /// <summary>Initializes a new instance of the <see cref="ProjectPlanningLinesController"/> class.</summary>
    public ProjectPlanningLinesController(
        IPlanningLineService planningLineService,
        ICacheService cacheService,
        ILogger<ProjectPlanningLinesController> logger)
        : base(cacheService, logger)
    {
        _planningLineService = planningLineService;
    }

    /// <summary>Export the planning lines of a project as XLSX.</summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ExportToXlsx(Guid projectId) => ExecuteAsync(async () =>
    {
        var lines = await _planningLineService.ListAsync(projectId, cancellationToken: HttpContext.RequestAborted);
        return File(lines.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlanningLines.xlsx");
    }, "exporting planning lines");

    /// <summary>Export the planning lines of a project as PDF.</summary>
    [HttpGet("export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportToPdf(Guid projectId) => ExecuteAsync(async () =>
    {
        var lines = await _planningLineService.ListAsync(projectId, cancellationToken: HttpContext.RequestAborted);
        return File(lines.ExportToPdf(), "application/pdf", "PlanningLines.pdf");
    }, "exporting planning lines");

    /// <summary>List the planning lines of a project, optionally filtered by task.</summary>
    [HttpGet]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<PlanningLineDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(Guid projectId, [FromQuery] Guid? taskId) => ExecuteAsync(
        async () => Ok(await _planningLineService.ListAsync(projectId, taskId, HttpContext.RequestAborted)),
        "retrieving planning lines");

    /// <summary>Get a planning line.</summary>
    [HttpGet("{lineId:guid}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(PlanningLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid projectId, Guid lineId) => ExecuteAsync(async () =>
    {
        var line = await _planningLineService.GetByIdAsync(projectId, lineId, HttpContext.RequestAborted);
        return line is null ? NotFound(new { message = $"Planning line {lineId} not found." }) : Ok(line);
    }, "retrieving the planning line");

    /// <summary>Create a planning line on a leaf Posting task.</summary>
    [HttpPost]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(PlanningLineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create(Guid projectId, [FromBody] CreateUpdatePlanningLineDto dto) => ExecuteAsync(async () =>
    {
        var line = await _planningLineService.CreateAsync(projectId, dto, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { projectId, lineId = line.Id }, line);
    }, "creating the planning line");

    /// <summary>Update a planning line.</summary>
    [HttpPut("{lineId:guid}")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(PlanningLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid projectId, Guid lineId, [FromBody] CreateUpdatePlanningLineDto dto) => ExecuteAsync(
        async () => Ok(await _planningLineService.UpdateAsync(projectId, lineId, dto, HttpContext.RequestAborted)),
        "updating the planning line");

    /// <summary>Delete a planning line (nothing posted, consumed or invoiced).</summary>
    [HttpDelete("{lineId:guid}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid projectId, Guid lineId) => ExecuteAsync(async () =>
    {
        await _planningLineService.DeleteAsync(projectId, lineId, HttpContext.RequestAborted);
        return NoContent();
    }, "deleting the planning line");
}
