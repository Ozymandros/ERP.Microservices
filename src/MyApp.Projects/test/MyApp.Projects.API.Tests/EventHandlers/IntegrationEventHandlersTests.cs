using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.API.EventHandlers;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Projects.API.Tests.EventHandlers;

/// <summary>Unit tests for order-related handlers in <c>IntegrationEventHandlers.cs</c>.</summary>
public class ProjectsOrderEventHandlersTests
{
    private readonly Mock<IProjectPostingService> _postingService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<OrderEventHandlers>> _logger = new();
    private readonly OrderEventHandlers _sut;

    public ProjectsOrderEventHandlersTests()
    {
        _sut = new OrderEventHandlers(_postingService.Object, _cacheService.Object, _logger.Object);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_WhenProjectIdReturned_InvalidatesProjectCacheAndReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var @event = SampleFulfilledEvent();

        _postingService
            .Setup(s => s.HandleOrderFulfilledAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectId);

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync($"project_{projectId}"), Times.Once);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_WhenProjectIdNull_SkipsCacheAndReturnsOk()
    {
        var @event = SampleFulfilledEvent();
        _postingService
            .Setup(s => s.HandleOrderFulfilledAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnOrderCancelledAsync_WhenProjectIdReturned_InvalidatesProjectCacheAndReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var @event = new OrderCancelledEvent(Guid.NewGuid(), "Cancelled", Guid.NewGuid(), "Project");

        _postingService
            .Setup(s => s.HandleOrderCancelledAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectId);

        var result = await _sut.OnOrderCancelledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync($"project_{projectId}"), Times.Once);
    }

    private static OrderFulfilledEvent SampleFulfilledEvent() =>
        new(
            Guid.NewGuid(),
            "OP-P",
            "Outbound",
            Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            [new OrderLineEvent(Guid.NewGuid(), 1)],
            "Project");
}

/// <summary>Unit tests for billing-related handlers in <c>IntegrationEventHandlers.cs</c>.</summary>
public class ProjectsBillingEventHandlersTests
{
    private readonly Mock<IProjectPostingService> _postingService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<BillingEventHandlers>> _logger = new();
    private readonly BillingEventHandlers _sut;

    public ProjectsBillingEventHandlersTests()
    {
        _sut = new BillingEventHandlers(_postingService.Object, _cacheService.Object, _logger.Object);
    }

    [Fact]
    public async Task OnInvoiceCreatedAsync_WhenProjectIdReturned_InvalidatesCacheAndReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var @event = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), null, "EUR", 100m, "Project", projectId);

        _postingService
            .Setup(s => s.HandleInvoiceCreatedAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectId);

        var result = await _sut.OnInvoiceCreatedAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync($"project_{projectId}"), Times.Once);
    }

    [Fact]
    public async Task OnInvoiceIssuedAsync_WhenProjectIdReturned_InvalidatesCacheAndReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var @event = new InvoiceIssuedEvent(
            Guid.NewGuid(),
            "INV-P",
            Guid.NewGuid(),
            null,
            80m,
            16m,
            96m,
            DateTime.UtcNow,
            "Project",
            projectId);

        _postingService
            .Setup(s => s.HandleInvoiceIssuedAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectId);

        var result = await _sut.OnInvoiceIssuedAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync($"project_{projectId}"), Times.Once);
    }

    [Fact]
    public async Task OnInvoiceCancelledAsync_WhenProjectIdReturned_InvalidatesCacheAndReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var @event = new InvoiceCancelledEvent(Guid.NewGuid(), "INV-C", "Error", "Project", projectId);

        _postingService
            .Setup(s => s.HandleInvoiceCancelledAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectId);

        var result = await _sut.OnInvoiceCancelledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync($"project_{projectId}"), Times.Once);
    }
}