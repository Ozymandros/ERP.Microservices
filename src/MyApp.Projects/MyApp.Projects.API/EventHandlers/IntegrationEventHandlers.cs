using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Projects.API.EventHandlers;

/// <summary>
/// Handles operational order events for project item consumption
/// (only orders with ExternalOrderSource = "Project" are processed).
/// </summary>
[ApiController]
[Route("api/events/orders")]
public class OrderEventHandlers : ControllerBase
{
    private readonly IProjectPostingService _postingService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderEventHandlers> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrderEventHandlers"/> class.</summary>
    public OrderEventHandlers(IProjectPostingService postingService, ICacheService cacheService, ILogger<OrderEventHandlers> logger)
    {
        _postingService = postingService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>Posts item usage when a project consumption order is fulfilled.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderFulfilled)]
    [HttpPost("order-fulfilled")]
    public async Task<IActionResult> OnOrderFulfilledAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Projects received OrderFulfilledEvent OrderId={OrderId} Source={Source}",
            @event.OrderId, @event.ExternalOrderSource);

        var projectId = await _postingService.HandleOrderFulfilledAsync(@event, cancellationToken);
        await InvalidateAsync(projectId);
        return Ok();
    }

    /// <summary>Releases pending consumption when a project consumption order is cancelled.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderCancelled)]
    [HttpPost("order-cancelled")]
    public async Task<IActionResult> OnOrderCancelledAsync(OrderCancelledEvent @event, CancellationToken cancellationToken)
    {
        var projectId = await _postingService.HandleOrderCancelledAsync(@event, cancellationToken);
        await InvalidateAsync(projectId);
        return Ok();
    }

    private async Task InvalidateAsync(Guid? projectId)
    {
        if (projectId.HasValue)
            await _cacheService.RemoveStateAsync($"project_{projectId}");
    }
}

/// <summary>
/// Handles Billing invoice events for project invoices (only invoices with SourceType = "Project" are processed).
/// </summary>
[ApiController]
[Route("api/events/billing")]
public class BillingEventHandlers : ControllerBase
{
    private readonly IProjectPostingService _postingService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BillingEventHandlers> _logger;

    /// <summary>Initializes a new instance of the <see cref="BillingEventHandlers"/> class.</summary>
    public BillingEventHandlers(IProjectPostingService postingService, ICacheService cacheService, ILogger<BillingEventHandlers> logger)
    {
        _postingService = postingService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>Links the draft invoice to the project invoice request.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.BillingInvoiceCreated)]
    [HttpPost("invoice-created")]
    public async Task<IActionResult> OnInvoiceCreatedAsync(InvoiceCreatedEvent @event, CancellationToken cancellationToken)
    {
        var projectId = await _postingService.HandleInvoiceCreatedAsync(@event, cancellationToken);
        await InvalidateAsync(projectId);
        return Ok();
    }

    /// <summary>Posts sale entries when a project invoice is issued.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.BillingInvoiceIssued)]
    [HttpPost("invoice-issued")]
    public async Task<IActionResult> OnInvoiceIssuedAsync(InvoiceIssuedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Projects received InvoiceIssuedEvent InvoiceId={InvoiceId} Source={Source}",
            @event.InvoiceId, @event.SourceType);

        var projectId = await _postingService.HandleInvoiceIssuedAsync(@event, cancellationToken);
        await InvalidateAsync(projectId);
        return Ok();
    }

    /// <summary>Reverses / releases quantities when a project invoice is cancelled.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.BillingInvoiceCancelled)]
    [HttpPost("invoice-cancelled")]
    public async Task<IActionResult> OnInvoiceCancelledAsync(InvoiceCancelledEvent @event, CancellationToken cancellationToken)
    {
        var projectId = await _postingService.HandleInvoiceCancelledAsync(@event, cancellationToken);
        await InvalidateAsync(projectId);
        return Ok();
    }

    private async Task InvalidateAsync(Guid? projectId)
    {
        if (projectId.HasValue)
            await _cacheService.RemoveStateAsync($"project_{projectId}");
    }
}
