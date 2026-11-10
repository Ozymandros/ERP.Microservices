using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Projects.API.Controllers;

/// <summary>Project cards: CRUD, status lifecycle, copy, statistics and export.</summary>
[ApiController]
[Authorize]
[Route("api/projects")]
[Produces("application/json")]
public class ProjectsController : ProjectsApiControllerBase
{
    private readonly IProjectService _projectService;

    /// <summary>Initializes a new instance of the <see cref="ProjectsController"/> class.</summary>
    public ProjectsController(IProjectService projectService, ICacheService cacheService, ILogger<ProjectsController> logger)
        : base(cacheService, logger)
    {
        _projectService = projectService;
    }

    /// <summary>Export all projects as XLSX.</summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public Task<IActionResult> ExportToXlsx() => ExecuteAsync(async () =>
    {
        var projects = await _projectService.ListAsync(HttpContext.RequestAborted);
        return File(projects.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Projects.xlsx");
    }, "exporting projects");

    /// <summary>Export all projects as PDF.</summary>
    [HttpGet("export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public Task<IActionResult> ExportToPdf() => ExecuteAsync(async () =>
    {
        var projects = await _projectService.ListAsync(HttpContext.RequestAborted);
        return File(projects.ExportToPdf(), "application/pdf", "Projects.pdf");
    }, "exporting projects");

    /// <summary>
    /// Get all projects. With query parameters (page, pageSize, sortBy, searchTerm, Status, CustomerId, CurrencyCode, ...)
    /// a paginated result is returned.
    /// </summary>
    [HttpGet]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<ProjectDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] QuerySpec query) => ExecuteAsync(async () =>
    {
        if (Request.Query.Any())
        {
            query.BindFiltersFromQuery(Request.Query);
            query.Validate();
            return Ok(await _projectService.QueryAsync(new ProjectQuerySpec(query), HttpContext.RequestAborted));
        }

        var cached = await CacheService.GetStateAsync<List<ProjectDto>>(AllProjectsCacheKey);
        if (cached is not null)
            return Ok(cached);

        var projects = await _projectService.ListAsync(HttpContext.RequestAborted);
        await CacheService.SaveStateAsync(AllProjectsCacheKey, projects, TimeSpan.FromMinutes(5));
        return Ok(projects);
    }, "retrieving projects");

    /// <summary>Get a project by id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) => ExecuteAsync(async () =>
    {
        var cached = await CacheService.GetStateAsync<ProjectDto>(ProjectCacheKey(id));
        if (cached is not null)
            return Ok(cached);

        var project = await _projectService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (project is null)
            return NotFound(new { message = $"Project {id} not found." });

        await CacheService.SaveStateAsync(ProjectCacheKey(id), project, TimeSpan.FromMinutes(5));
        return Ok(project);
    }, "retrieving the project");

    /// <summary>Get a project by number.</summary>
    [HttpGet("number/{projectNumber}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByNumber(string projectNumber) => ExecuteAsync(async () =>
    {
        var project = await _projectService.GetByNumberAsync(projectNumber, HttpContext.RequestAborted);
        return project is null ? NotFound(new { message = $"Project {projectNumber} not found." }) : Ok(project);
    }, "retrieving the project");

    /// <summary>Create a project (status Planning). The customer must exist in Sales.</summary>
    [HttpPost]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] CreateProjectDto dto) => ExecuteAsync(async () =>
    {
        var project = await _projectService.CreateAsync(dto, HttpContext.RequestAborted);
        await CacheService.RemoveStateAsync(AllProjectsCacheKey);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }, "creating the project");

    /// <summary>Update a project header.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectDto dto) => ExecuteAsync(async () =>
    {
        var project = await _projectService.UpdateAsync(id, dto, HttpContext.RequestAborted);
        await InvalidateProjectAsync(id);
        return Ok(project);
    }, "updating the project");

    /// <summary>Delete a project (Planning/Quote only, no ledger entries).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id) => ExecuteAsync(async () =>
    {
        await _projectService.DeleteAsync(id, HttpContext.RequestAborted);
        await InvalidateProjectAsync(id);
        return NoContent();
    }, "deleting the project");

    /// <summary>
    /// Change the project status. Allowed: Planning→Quote/Open, Quote→Planning/Open, Open→Completed, Completed→Open.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [HasPermission("Projects", "Execute")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeProjectStatusDto dto) => ExecuteAsync(async () =>
    {
        var project = await _projectService.ChangeStatusAsync(id, dto.Status, HttpContext.RequestAborted);
        await InvalidateProjectAsync(id);
        return Ok(project);
    }, "changing the project status");

    /// <summary>Copy a project (tasks and, optionally, planning lines) into a new project in Planning.</summary>
    [HttpPost("{id:guid}/copy")]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Copy(Guid id, [FromBody] CopyProjectDto dto) => ExecuteAsync(async () =>
    {
        var project = await _projectService.CopyAsync(id, dto, HttpContext.RequestAborted);
        await CacheService.RemoveStateAsync(AllProjectsCacheKey);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }, "copying the project");

    /// <summary>Get project statistics (budget vs. usage vs. invoiced), rolled up through the task tree.</summary>
    [HttpGet("{id:guid}/statistics")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(ProjectStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetStatistics(Guid id) => ExecuteAsync(
        async () => Ok(await _projectService.GetStatisticsAsync(id, HttpContext.RequestAborted)),
        "retrieving project statistics");
}
