using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Billing.API.EventHandlers;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Billing.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="ProjectEventHandlers"/>.</summary>
public class ProjectEventHandlersTests
{
    private readonly Mock<IInvoiceService> _invoiceService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<ProjectEventHandlers>> _logger = new();

    [Fact]
    public async Task OnProjectInvoiceRequestedAsync_WithInvoice_InvalidatesCacheAndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var invoice = new InvoiceDto(
            Guid.NewGuid(),
            "P-INV-1",
            customerId,
            null,
            "EUR",
            "Draft",
            null,
            null,
            200m,
            40m,
            240m,
            240m,
            [],
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Project",
            Guid.NewGuid());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ProjectEventHandlers.AutoIssueConfigKey] = "false"
            })
            .Build();

        var sut = new ProjectEventHandlers(
            _invoiceService.Object,
            _cacheService.Object,
            configuration,
            _logger.Object);

        var @event = new ProjectInvoiceRequestedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PRJ-1",
            customerId,
            "EUR",
            []);

        _invoiceService
            .Setup(s => s.CreateFromProjectRequestAsync(@event, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var result = await sut.OnProjectInvoiceRequestedAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _invoiceService.Verify(
            s => s.CreateFromProjectRequestAsync(@event, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoice_{invoice.Id}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("open_invoices"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoices_customer_{customerId}"), Times.Once);
    }

    [Fact]
    public async Task OnProjectInvoiceRequestedAsync_WhenNoInvoice_SkipsCacheAndReturnsOk()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new ProjectEventHandlers(
            _invoiceService.Object,
            _cacheService.Object,
            configuration,
            _logger.Object);

        var @event = new ProjectInvoiceRequestedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PRJ-2",
            Guid.NewGuid(),
            "USD",
            []);

        _invoiceService
            .Setup(s => s.CreateFromProjectRequestAsync(@event, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceDto?)null);

        var result = await sut.OnProjectInvoiceRequestedAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }
}