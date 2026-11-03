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

/// <summary>Project resources (persons and machines).</summary>
[ApiController]
[Authorize]
[Route("api/projects/resources")]
[Produces("application/json")]
public class ResourcesController : ProjectsApiControllerBase
{
    private const string AllResourcesCacheKey = "all_project_resources";
    private readonly IResourceService _resourceService;

    /// <summary>Initializes a new instance of the <see cref="ResourcesController"/> class.</summary>
    public ResourcesController(IResourceService resourceService, ICacheService cacheService, ILogger<ResourcesController> logger)
        : base(cacheService, logger)
    {
        _resourceService = resourceService;
    }

    /// <summary>Export all resources as XLSX.</summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ExportToXlsx() => ExecuteAsync(async () =>
    {
        var resources = await _resourceService.ListAsync(HttpContext.RequestAborted);
        return File(resources.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Resources.xlsx");
    }, "exporting resources");

    /// <summary>Export all resources as PDF.</summary>
    [HttpGet("export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportToPdf() => ExecuteAsync(async () =>
    {
        var resources = await _resourceService.ListAsync(HttpContext.RequestAborted);
        return File(resources.ExportToPdf(), "application/pdf", "Resources.pdf");
    }, "exporting resources");

    /// <summary>Get all resources (paginated when query parameters are supplied).</summary>
    [HttpGet]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(IEnumerable<ResourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<ResourceDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] QuerySpec query) => ExecuteAsync(async () =>
    {
        if (Request.Query.Any())
        {
            query.BindFiltersFromQuery(Request.Query);
            query.Validate();
            return Ok(await _resourceService.QueryAsync(new ResourceQuerySpec(query), HttpContext.RequestAborted));
        }

        var cached = await CacheService.GetStateAsync<List<ResourceDto>>(AllResourcesCacheKey);
        if (cached is not null)
            return Ok(cached);

        var resources = await _resourceService.ListAsync(HttpContext.RequestAborted);
        await CacheService.SaveStateAsync(AllResourcesCacheKey, resources, TimeSpan.FromMinutes(5));
        return Ok(resources);
    }, "retrieving resources");

    /// <summary>Get a resource.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) => ExecuteAsync(async () =>
    {
        var resource = await _resourceService.GetByIdAsync(id, HttpContext.RequestAborted);
        return resource is null ? NotFound(new { message = $"Resource {id} not found." }) : Ok(resource);
    }, "retrieving the resource");

    /// <summary>Create a resource.</summary>
    [HttpPost]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] CreateUpdateResourceDto dto) => ExecuteAsync(async () =>
    {
        var resource = await _resourceService.CreateAsync(dto, HttpContext.RequestAborted);
        await CacheService.RemoveStateAsync(AllResourcesCacheKey);
        return CreatedAtAction(nameof(GetById), new { id = resource.Id }, resource);
    }, "creating the resource");

    /// <summary>Update a resource.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateResourceDto dto) => ExecuteAsync(async () =>
    {
        var resource = await _resourceService.UpdateAsync(id, dto, HttpContext.RequestAborted);
        await CacheService.RemoveStateAsync(AllResourcesCacheKey);
        return Ok(resource);
    }, "updating the resource");

    /// <summary>Delete a resource (only when unused; otherwise block it).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id) => ExecuteAsync(async () =>
    {
        await _resourceService.DeleteAsync(id, HttpContext.RequestAborted);
        await CacheService.RemoveStateAsync(AllResourcesCacheKey);
        return NoContent();
    }, "deleting the resource");
}

/// <summary>Resource groups.</summary>
[ApiController]
[Authorize]
[Route("api/projects/resource-groups")]
[Produces("application/json")]
public class ResourceGroupsController : ProjectsApiControllerBase
{
    private readonly IResourceService _resourceService;

    /// <summary>Initializes a new instance of the <see cref="ResourceGroupsController"/> class.</summary>
    public ResourceGroupsController(IResourceService resourceService, ICacheService cacheService, ILogger<ResourceGroupsController> logger)
        : base(cacheService, logger)
    {
        _resourceService = resourceService;
    }

    /// <summary>Export all resource groups as XLSX.</summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ExportToXlsx() => ExecuteAsync(async () =>
    {
        var groups = await _resourceService.ListGroupsAsync(HttpContext.RequestAborted);
        return File(groups.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ResourceGroups.xlsx");
    }, "exporting resource groups");

    /// <summary>Export all resource groups as PDF.</summary>
    [HttpGet("export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportToPdf() => ExecuteAsync(async () =>
    {
        var groups = await _resourceService.ListGroupsAsync(HttpContext.RequestAborted);
        return File(groups.ExportToPdf(), "application/pdf", "ResourceGroups.pdf");
    }, "exporting resource groups");

    /// <summary>List resource groups.</summary>
    [HttpGet]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<ResourceGroupDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() => ExecuteAsync(
        async () => Ok(await _resourceService.ListGroupsAsync(HttpContext.RequestAborted)),
        "retrieving resource groups");

    /// <summary>Get a resource group.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(ResourceGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) => ExecuteAsync(async () =>
    {
        var group = await _resourceService.GetGroupByIdAsync(id, HttpContext.RequestAborted);
        return group is null ? NotFound(new { message = $"Resource group {id} not found." }) : Ok(group);
    }, "retrieving the resource group");

    /// <summary>Create a resource group.</summary>
    [HttpPost]
    [HasPermission("Projects", "Create")]
    [ProducesResponseType(typeof(ResourceGroupDto), StatusCodes.Status201Created)]
    public Task<IActionResult> Create([FromBody] CreateUpdateResourceGroupDto dto) => ExecuteAsync(async () =>
    {
        var group = await _resourceService.CreateGroupAsync(dto, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }, "creating the resource group");

    /// <summary>Update a resource group.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("Projects", "Update")]
    [ProducesResponseType(typeof(ResourceGroupDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateResourceGroupDto dto) => ExecuteAsync(
        async () => Ok(await _resourceService.UpdateGroupAsync(id, dto, HttpContext.RequestAborted)),
        "updating the resource group");

    /// <summary>Delete a resource group (not in use).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("Projects", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Delete(Guid id) => ExecuteAsync(async () =>
    {
        await _resourceService.DeleteGroupAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }, "deleting the resource group");
}
