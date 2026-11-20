using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Billing.API.EventHandlers;

/// <summary>Handles events published by the Projects service.</summary>
[ApiController]
[Route("api/events/projects")]
public class ProjectEventHandlers : ControllerBase
{
    /// <summary>Configuration key that enables auto-issuing of project invoices (default: false = Draft).</summary>
    public const string AutoIssueConfigKey = "Billing:ProjectInvoices:AutoIssue";

    private readonly IInvoiceService _invoiceService;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProjectEventHandlers> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProjectEventHandlers"/> class.</summary>
    public ProjectEventHandlers(
        IInvoiceService invoiceService,
        ICacheService cacheService,
        IConfiguration configuration,
        ILogger<ProjectEventHandlers> logger)
    {
        _invoiceService = invoiceService;
        _cacheService = cacheService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Creates a customer invoice (Draft by default) from billable project planning lines.
    /// Set <c>Billing:ProjectInvoices:AutoIssue=true</c> to issue it immediately.
    /// </summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.ProjectInvoiceRequested)]
    [HttpPost("invoice-requested")]
    public async Task<IActionResult> OnProjectInvoiceRequestedAsync(
        ProjectInvoiceRequestedEvent @event,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Billing received ProjectInvoiceRequestedEvent RequestId={RequestId} ProjectId={ProjectId} Lines={LineCount}",
            @event.RequestId,
            @event.ProjectId,
            @event.Lines.Count);

        var autoIssue = _configuration.GetValue<bool>(AutoIssueConfigKey);
        var invoice = await _invoiceService.CreateFromProjectRequestAsync(@event, autoIssue, cancellationToken);

        if (invoice is not null)
        {
            await _cacheService.RemoveStateAsync($"invoice_{invoice.Id}");
            await _cacheService.RemoveStateAsync("open_invoices");
            await _cacheService.RemoveStateAsync($"invoices_customer_{invoice.CustomerId}");
        }

        return Ok();
    }
}
