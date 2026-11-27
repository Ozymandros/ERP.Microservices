using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Crm.API.EventHandlers;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Crm.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="SalesEventHandlers"/>.</summary>
public class SalesEventHandlersTests
{
    private readonly Mock<ILogger<SalesEventHandlers>> _logger = new();
    private readonly Mock<IAccountService> _accountService = new();
    private readonly SalesEventHandlers _sut;

    public SalesEventHandlersTests()
    {
        _sut = new SalesEventHandlers(_logger.Object, _accountService.Object);
    }

    [Fact]
    public async Task OnSalesCustomerCreated_UpsertsAccountAndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var @event = new SalesCustomerCreatedEvent(customerId, "Acme Corp", "sales@acme.test");

        var result = await _sut.OnSalesCustomerCreated(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _accountService.Verify(
            s => s.UpsertFromSalesAsync(
                It.Is<UpsertAccountDto>(d =>
                    d.CustomerId == customerId &&
                    d.Name == @event.Name),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnSalesCustomerUpdated_UpsertsAccountAndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var @event = new SalesCustomerUpdatedEvent(customerId, "Acme Updated", "new@acme.test");

        var result = await _sut.OnSalesCustomerUpdated(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _accountService.Verify(
            s => s.UpsertFromSalesAsync(
                It.Is<UpsertAccountDto>(d =>
                    d.CustomerId == customerId &&
                    d.Name == @event.Name),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}