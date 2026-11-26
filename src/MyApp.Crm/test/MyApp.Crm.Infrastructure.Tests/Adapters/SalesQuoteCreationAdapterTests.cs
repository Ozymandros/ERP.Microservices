using FluentAssertions;
using Moq;
using MyApp.Crm.Application.Contracts.Ports;
using MyApp.Crm.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Crm.Infrastructure.Tests.Adapters;

public class SalesQuoteCreationAdapterTests
{
    [Fact]
    public async Task CreateQuoteAsync_MapsWireResponseToSnapshot()
    {
        var quoteId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesQuoteCreationAdapter.CreateQuoteWire, SalesQuoteCreationAdapter.SalesQuoteWire>(
                ServiceNames.Sales,
                ApiEndpoints.Sales.Quotes,
                HttpMethod.Post,
                It.IsAny<SalesQuoteCreationAdapter.CreateQuoteWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesQuoteCreationAdapter.SalesQuoteWire
            {
                Id = quoteId,
                OrderNumber = "Q-100"
            });

        var sut = new SalesQuoteCreationAdapter(invoker.Object);
        var result = await sut.CreateQuoteAsync(
            new CreateSalesQuoteRequest(
                "Q-100",
                customerId,
                DateTime.UtcNow,
                30,
                [new CrmQuoteLineRequest(productId, 2, 15.5m)]));

        result.Id.Should().Be(quoteId);
        result.OrderNumber.Should().Be("Q-100");
        invoker.Verify(i => i.InvokeAsync<SalesQuoteCreationAdapter.CreateQuoteWire, SalesQuoteCreationAdapter.SalesQuoteWire>(
            ServiceNames.Sales,
            ApiEndpoints.Sales.Quotes,
            HttpMethod.Post,
            It.Is<SalesQuoteCreationAdapter.CreateQuoteWire>(w =>
                w.OrderNumber == "Q-100" &&
                w.CustomerId == customerId &&
                w.Lines != null &&
                w.Lines.Count == 1 &&
                w.Lines[0].ProductId == productId &&
                w.Lines[0].Quantity == 2 &&
                w.Lines[0].UnitPrice == 15.5m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateQuoteAsync_NullOrderNumber_MapsToEmptyString()
    {
        var quoteId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<SalesQuoteCreationAdapter.CreateQuoteWire, SalesQuoteCreationAdapter.SalesQuoteWire>(
                ServiceNames.Sales,
                ApiEndpoints.Sales.Quotes,
                HttpMethod.Post,
                It.IsAny<SalesQuoteCreationAdapter.CreateQuoteWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesQuoteCreationAdapter.SalesQuoteWire
            {
                Id = quoteId,
                OrderNumber = null
            });

        var sut = new SalesQuoteCreationAdapter(invoker.Object);
        var result = await sut.CreateQuoteAsync(
            new CreateSalesQuoteRequest(
                "Q-101",
                Guid.NewGuid(),
                DateTime.UtcNow,
                14,
                [new CrmQuoteLineRequest(Guid.NewGuid(), 1, 1m)]));

        result.OrderNumber.Should().BeEmpty();
    }
}
