using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Projects.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Projects.Infrastructure.Tests.Adapters;

public class SalesCustomerLookupAdapterTests
{
    [Fact]
    public async Task GetCustomerAsync_MapsWireToSnapshot()
    {
        var customerId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesCustomerLookupAdapter.CustomerWire>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Customers}/{customerId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesCustomerLookupAdapter.CustomerWire
            {
                Id = customerId,
                Name = "Acme"
            });

        var sut = new SalesCustomerLookupAdapter(invoker.Object, NullLogger<SalesCustomerLookupAdapter>.Instance);
        var result = await sut.GetCustomerAsync(customerId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(customerId);
        result.Name.Should().Be("Acme");
    }

    [Fact]
    public async Task GetCustomerAsync_NullName_MapsToEmptyString()
    {
        var customerId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesCustomerLookupAdapter.CustomerWire>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Customers}/{customerId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesCustomerLookupAdapter.CustomerWire { Id = customerId, Name = null });

        var sut = new SalesCustomerLookupAdapter(invoker.Object, NullLogger<SalesCustomerLookupAdapter>.Instance);
        var result = await sut.GetCustomerAsync(customerId);

        result!.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomerAsync_NotFound_ReturnsNull()
    {
        var customerId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesCustomerLookupAdapter.CustomerWire>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Customers}/{customerId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("missing", null, HttpStatusCode.NotFound));

        var sut = new SalesCustomerLookupAdapter(invoker.Object, NullLogger<SalesCustomerLookupAdapter>.Instance);
        var result = await sut.GetCustomerAsync(customerId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerAsync_OtherError_ThrowsInvalidOperationException()
    {
        var customerId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesCustomerLookupAdapter.CustomerWire>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Customers}/{customerId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom", null, HttpStatusCode.InternalServerError));

        var sut = new SalesCustomerLookupAdapter(invoker.Object, NullLogger<SalesCustomerLookupAdapter>.Instance);
        var act = async () => await sut.GetCustomerAsync(customerId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{customerId}*");
    }
}
