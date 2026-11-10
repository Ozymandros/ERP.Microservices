using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Projects.API.Controllers;

/// <summary>
/// Base controller for the Projects API: maps domain exceptions to HTTP results
/// (NotFound → 404, validation/business rule → 400, anything else → 500) and centralises cache invalidation.
/// </summary>
public abstract class ProjectsApiControllerBase : ControllerBase
{
    /// <summary>Cache key of the project list.</summary>
    protected const string AllProjectsCacheKey = "all_projects";

    /// <summary>Gets the cache service.</summary>
    protected ICacheService CacheService { get; }

    /// <summary>Gets the logger.</summary>
    protected ILogger Logger { get; }

    /// <summary>Initializes a new instance of the <see cref="ProjectsApiControllerBase"/> class.</summary>
    protected ProjectsApiControllerBase(ICacheService cacheService, ILogger logger)
    {
        CacheService = cacheService;
        Logger = logger;
    }

    /// <summary>Cache key of a single project.</summary>
    protected static string ProjectCacheKey(Guid projectId) => $"project_{projectId}";

    /// <summary>Executes an action translating exceptions into HTTP responses.</summary>
    /// <param name="action">The action.</param>
    /// <param name="operation">A short description used in logs and 500 responses.</param>
    protected async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action, string operation)
    {
        try
        {
            return await action();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error {Operation}", operation);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An error occurred {operation}" });
        }
    }

    /// <summary>Invalidates the cached data of a project.</summary>
    /// <param name="projectId">The project id.</param>
    protected async Task InvalidateProjectAsync(Guid projectId)
    {
        try
        {
            await CacheService.RemoveStateAsync(ProjectCacheKey(projectId));
            await CacheService.RemoveStateAsync(AllProjectsCacheKey);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Cache invalidation failed for project {ProjectId}", projectId);
        }
    }
}
