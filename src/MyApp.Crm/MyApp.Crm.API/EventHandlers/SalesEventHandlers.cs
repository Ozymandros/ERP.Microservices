using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Crm.API.EventHandlers;

/// <summary>
/// Provides Sales Event Handlers functionality.
/// </summary>
[ApiController]
[Route("api/events/sales")]
public class SalesEventHandlers : ControllerBase
{
    private readonly ILogger<SalesEventHandlers> _logger;
    private readonly IAccountService _accountService;

    public SalesEventHandlers(ILogger<SalesEventHandlers> logger, IAccountService accountService)
    {
        _logger = logger;
        _accountService = accountService;
    }

    /// <summary>On Sales Customer Created.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.SalesCustomerCreated)]
    [HttpPost("customer-created")]
    public async Task<IActionResult> OnSalesCustomerCreated(SalesCustomerCreatedEvent @event, CancellationToken cancellationToken)
    {
        await _accountService.UpsertFromSalesAsync(
            new UpsertAccountDto(
                CustomerId: @event.CustomerId,
                Name: @event.Name,
                TaxId: null,
                BillingAddress: null,
                ShippingAddress: null,
                SyncedAt: DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Synced CRM account snapshot from SalesCustomerCreatedEvent CustomerId={CustomerId}", @event.CustomerId);
        return Ok();
    }

    /// <summary>On Sales Customer Updated.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.SalesCustomerUpdated)]
    [HttpPost("customer-updated")]
    public async Task<IActionResult> OnSalesCustomerUpdated(SalesCustomerUpdatedEvent @event, CancellationToken cancellationToken)
    {
        await _accountService.UpsertFromSalesAsync(
            new UpsertAccountDto(
                CustomerId: @event.CustomerId,
                Name: @event.Name,
                TaxId: null,
                BillingAddress: null,
                ShippingAddress: null,
                SyncedAt: DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Synced CRM account snapshot from SalesCustomerUpdatedEvent CustomerId={CustomerId}", @event.CustomerId);
        return Ok();
    }
}

