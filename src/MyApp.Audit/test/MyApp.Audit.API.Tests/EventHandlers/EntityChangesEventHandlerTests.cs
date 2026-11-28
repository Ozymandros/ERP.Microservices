using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Audit.API.EventHandlers;
using MyApp.Audit.Application.Contracts.Services;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Audit.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="EntityChangesEventHandler"/>.</summary>
public class EntityChangesEventHandlerTests
{
    private readonly Mock<IEntityChangeService> _entityChangeService = new();
    private readonly Mock<ILogger<EntityChangesEventHandler>> _logger = new();
    private readonly EntityChangesEventHandler _sut;

    public EntityChangesEventHandlerTests()
    {
        _sut = new EntityChangesEventHandler(_entityChangeService.Object, _logger.Object);
    }

    [Fact]
    public async Task OnEntityChangesSaved_WhenNoChanges_ReturnsOkWithoutRecording()
    {
        var @event = new EntityChangesSavedEvent("inventory-service", []);

        var result = await _sut.OnEntityChangesSaved(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _entityChangeService.Verify(
            s => s.RecordFromEventAsync(It.IsAny<EntityChangesSavedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnEntityChangesSaved_WhenChangesPresent_RecordsAndReturnsOk()
    {
        var change = new EntityChangePayload(
            "Product",
            Guid.NewGuid(),
            "Modified",
            [new PropertyChangePayload("Name", "Old", "New")]);
        var @event = new EntityChangesSavedEvent("sales-service", [change]);

        var result = await _sut.OnEntityChangesSaved(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _entityChangeService.Verify(
            s => s.RecordFromEventAsync(@event, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}