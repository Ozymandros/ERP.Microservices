using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Crm.API.EventHandlers;

[ApiController]
[Route("api/events/sales")]
public class SalesEventHandlers : ControllerBase
{
    private readonly ILogger<SalesEventHandlers> _logger;

    public SalesEventHandlers(ILogger<SalesEventHandlers> logger)
    {
        _logger = logger;
    }

    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.SalesCustomerCreated)]
    [HttpPost("customer-created")]
    public IActionResult OnSalesCustomerCreated(SalesCustomerCreatedEvent @event)
    {
        // Iteration 1: log only. In a later increment we can maintain a CRM read-model snapshot.
        _logger.LogInformation(
            "Received SalesCustomerCreatedEvent: CustomerId={CustomerId}, Name={Name}",
            @event.CustomerId, @event.Name);
        return Ok();
    }

    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.SalesCustomerUpdated)]
    [HttpPost("customer-updated")]
    public IActionResult OnSalesCustomerUpdated(SalesCustomerUpdatedEvent @event)
    {
        _logger.LogInformation(
            "Received SalesCustomerUpdatedEvent: CustomerId={CustomerId}, Name={Name}",
            @event.CustomerId, @event.Name);
        return Ok();
    }
}

