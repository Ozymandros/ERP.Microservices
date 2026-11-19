using FluentAssertions;
using Moq;
using MyApp.Purchasing.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Purchasing.Infrastructure.Tests.Adapters;

public class SupplierInvoiceQueryAdapterTests
{
    [Fact]
    public async Task GetByPurchaseOrderIdAsync_MapsWireInvoicesToSnapshots()
    {
        var poId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<List<SupplierInvoiceQueryAdapter.SupplierInvoiceWire>>(
                ServiceNames.Billing,
                $"{ApiEndpoints.Billing.SupplierInvoices}/by-purchase-order/{poId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SupplierInvoiceQueryAdapter.SupplierInvoiceWire
                {
                    Id = Guid.NewGuid(),
                    Lines = [new SupplierInvoiceQueryAdapter.SupplierInvoiceLineWire { ProductId = productId, Quantity = 3 }]
                }
            ]);

        var sut = new SupplierInvoiceQueryAdapter(invoker.Object);
        var result = await sut.GetByPurchaseOrderIdAsync(poId);

        result.Should().HaveCount(1);
        result[0].Lines.Should().ContainSingle(l => l.ProductId == productId && l.Quantity == 3);
    }
}