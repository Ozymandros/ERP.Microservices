using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Audit.Application.Contracts.Services;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
namespace MyApp.Audit.API.EventHandlers;

/// <summary>Sole subscriber for entity-change audit events from producer microservices.</summary>
[ApiController]
[Route("api/events/audit")]
public class EntityChangesEventHandler : ControllerBase
{
    private readonly IEntityChangeService _entityChangeService;
    private readonly ILogger<EntityChangesEventHandler> _logger;

    public EntityChangesEventHandler(
        IEntityChangeService entityChangeService,
        ILogger<EntityChangesEventHandler> logger)
    {
        _entityChangeService = entityChangeService;
        _logger = logger;
    }

    /// <summary>Ingests committed entity changes from a producer service.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.AuditEntityChangesSaved)]
    [HttpPost("entity-changes-saved")]
    public async Task<IActionResult> OnEntityChangesSaved(
        EntityChangesSavedEvent @event,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received EntityChangesSavedEvent from {SourceService} with {ChangeCount} changes",
            @event.SourceService,
            @event.Changes.Count);

        if (@event.Changes.Count == 0)
            return Ok();

        await _entityChangeService.RecordFromEventAsync(@event, cancellationToken);

        _logger.LogInformation(
            "Ingested {ChangeCount} entity changes from {SourceService}",
            @event.Changes.Count,
            @event.SourceService);

        return Ok();
    }
}
