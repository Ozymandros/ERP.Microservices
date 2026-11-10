using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Projects.API.Controllers;

/// <summary>
/// Project posting: ledger entries, usage, item consumption (through Orders) and invoice requests (through Billing).
/// </summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}")]
[Produces("application/json")]
public class ProjectPostingController : ProjectsApiControllerBase
{
    private readonly IProjectPostingService _postingService;

    /// <summary>Initializes a new instance of the <see cref="ProjectPostingController"/> class.</summary>
    public ProjectPostingController(
        IProjectPostingService postingService,
        ICacheService cacheService,
        ILogger<ProjectPostingController> logger)
        : base(cacheService, logger)
    {
        _postingService = postingService;
    }

    /// <summary>Export the ledger entries of a project as XLSX.</summary>
    [HttpGet("ledger-entries/export-xlsx")]
    [HasPermission("Projects", "Export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public Task<IActionResult> ExportLedgerToXlsx(Guid projectId) => ExecuteAsync(async () =>
    {
        var entries = await _postingService.GetLedgerEntriesAsync(projectId, cancellationToken: HttpContext.RequestAborted);
        return File(entries.ExportToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProjectLedgerEntries.xlsx");
    }, "exporting ledger entries");

    /// <summary>Export the ledger entries of a project as PDF.</summary>
    [HttpGet("ledger-entries/export-pdf")]
    [HasPermission("Projects", "Export")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportLedgerToPdf(Guid projectId) => ExecuteAsync(async () =>
    {
        var entries = await _postingService.GetLedgerEntriesAsync(projectId, cancellationToken: HttpContext.RequestAborted);
        return File(entries.ExportToPdf(), "application/pdf", "ProjectLedgerEntries.pdf");
    }, "exporting ledger entries");

    /// <summary>List the ledger entries of a project, optionally filtered by task.</summary>
    [HttpGet("ledger-entries")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<ProjectLedgerEntryDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetLedgerEntries(Guid projectId, [FromQuery] Guid? taskId) => ExecuteAsync(
        async () => Ok(await _postingService.GetLedgerEntriesAsync(projectId, taskId, HttpContext.RequestAborted)),
        "retrieving ledger entries");

    /// <summary>Post resource or expense usage (the project must be Open and not blocked).</summary>
    [HttpPost("usage")]
    [HasPermission("Projects", "Execute")]
    [ProducesResponseType(typeof(ProjectLedgerEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> PostUsage(Guid projectId, [FromBody] PostUsageDto dto) => ExecuteAsync(
        async () => Ok(await _postingService.PostUsageAsync(projectId, dto, HttpContext.RequestAborted)),
        "posting usage");

    /// <summary>
    /// Consume items: creates an Outbound order in Orders (ExternalOrderSource = Project). Usage entries are
    /// posted when Orders publishes <c>orders.order.fulfilled</c>.
    /// </summary>
    [HttpPost("item-consumption")]
    [HasPermission("Projects", "Execute")]
    [ProducesResponseType(typeof(ConsumptionOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> CreateItemConsumption(Guid projectId, [FromBody] CreateItemConsumptionDto dto) => ExecuteAsync(
        async () => Ok(await _postingService.CreateItemConsumptionAsync(projectId, dto, HttpContext.RequestAborted)),
        "creating the item consumption order");

    /// <summary>List the item consumption orders of a project.</summary>
    [HttpGet("item-consumption")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<ConsumptionOrderDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetConsumptionOrders(Guid projectId) => ExecuteAsync(
        async () => Ok(await _postingService.GetConsumptionOrdersAsync(projectId, HttpContext.RequestAborted)),
        "retrieving consumption orders");

    /// <summary>
    /// Request a customer invoice from billable planning lines. Published to Billing via pub/sub;
    /// Billing creates a Draft invoice unless auto-issue is configured.
    /// </summary>
    [HttpPost("invoice-requests")]
    [HasPermission("Projects", "Execute")]
    [ProducesResponseType(typeof(InvoiceRequestDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> RequestInvoice(Guid projectId, [FromBody] CreateInvoiceRequestDto dto) => ExecuteAsync(
        async () => Accepted(await _postingService.RequestInvoiceAsync(projectId, dto, HttpContext.RequestAborted)),
        "requesting the invoice");

    /// <summary>List the invoice requests of a project.</summary>
    [HttpGet("invoice-requests")]
    [HasPermission("Projects", "Read")]
    [ProducesResponseType(typeof(List<InvoiceRequestDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetInvoiceRequests(Guid projectId) => ExecuteAsync(
        async () => Ok(await _postingService.GetInvoiceRequestsAsync(projectId, HttpContext.RequestAborted)),
        "retrieving invoice requests");
}
