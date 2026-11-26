using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Billing.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Billing.Infrastructure.Tests.Adapters;

/// <summary>Tests for <see cref="SalesOrderLookupAdapter"/>.</summary>
public class SalesOrderLookupAdapterTests
{
    [Fact]
    public async Task GetByIdAsync_MapsWireDtoToSnapshot()
    {
        var salesOrderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesOrderWireDto>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesOrderWireDto
            {
                Id = salesOrderId,
                OrderNumber = "SO-42",
                CustomerId = customerId,
                Lines =
                [
                    new SalesOrderLineWireDto
                    {
                        ProductId = productId,
                        Quantity = 3,
                        UnitPrice = 12.5m
                    }
                ]
            });

        var sut = new SalesOrderLookupAdapter(invoker.Object, NullLogger<SalesOrderLookupAdapter>.Instance);

        var snapshot = await sut.GetByIdAsync(salesOrderId);

        snapshot.Should().NotBeNull();
        snapshot!.Id.Should().Be(salesOrderId);
        snapshot.OrderNumber.Should().Be("SO-42");
        snapshot.CustomerId.Should().Be(customerId);
        snapshot.Lines.Should().ContainSingle(l =>
            l.ProductId == productId && l.Quantity == 3 && l.UnitPrice == 12.5m);
    }

    [Fact]
    public async Task GetByIdAsync_NullOrderNumberAndLines_MapsDefaults()
    {
        var salesOrderId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesOrderWireDto>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesOrderWireDto
            {
                Id = salesOrderId,
                OrderNumber = null,
                CustomerId = Guid.NewGuid(),
                Lines = null
            });

        var sut = new SalesOrderLookupAdapter(invoker.Object, NullLogger<SalesOrderLookupAdapter>.Instance);
        var snapshot = await sut.GetByIdAsync(salesOrderId);

        snapshot!.OrderNumber.Should().BeEmpty();
        snapshot.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var salesOrderId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesOrderWireDto>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("missing", null, System.Net.HttpStatusCode.NotFound));

        var sut = new SalesOrderLookupAdapter(invoker.Object, NullLogger<SalesOrderLookupAdapter>.Instance);
        var snapshot = await sut.GetByIdAsync(salesOrderId);

        snapshot.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NestedNotFound_ReturnsNull()
    {
        var salesOrderId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesOrderWireDto>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(
                "wrap",
                new HttpRequestException("missing", null, System.Net.HttpStatusCode.NotFound)));

        var sut = new SalesOrderLookupAdapter(invoker.Object, NullLogger<SalesOrderLookupAdapter>.Instance);
        var snapshot = await sut.GetByIdAsync(salesOrderId);

        snapshot.Should().BeNull();
    }
}
