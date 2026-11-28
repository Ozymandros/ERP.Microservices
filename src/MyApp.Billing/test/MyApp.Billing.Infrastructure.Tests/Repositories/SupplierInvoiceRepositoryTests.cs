using FluentAssertions;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Infrastructure.Repositories;
using MyApp.Billing.Infrastructure.Tests.Helpers;
using Xunit;

namespace MyApp.Billing.Infrastructure.Tests.Repositories;

public class SupplierInvoiceRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_IncludesLines()
    {
        using var ctx = TestDbContextFactory.Create();
        var poId = Guid.NewGuid();
        var invoice = new SupplierInvoice(Guid.NewGuid())
        {
            InvoiceNumber = "SINV-1",
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = poId,
            Currency = "USD",
            Lines =
            [
                new SupplierInvoiceLine(Guid.NewGuid())
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    UnitPrice = 15m
                }
            ]
        };
        invoice.Lines[0].SupplierInvoiceId = invoice.Id;
        ctx.SupplierInvoices.Add(invoice);
        ctx.SaveChanges();
        var sut = new SupplierInvoiceRepository(ctx);

        var result = await sut.GetByIdAsync(invoice.Id);

        result.Should().NotBeNull();
        result!.Lines.Should().ContainSingle(l => l.Quantity == 2);
    }

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_ReturnsInvoicesForPo()
    {
        using var ctx = TestDbContextFactory.Create();
        var poId = Guid.NewGuid();
        ctx.SupplierInvoices.Add(new SupplierInvoice(Guid.NewGuid())
        {
            InvoiceNumber = "SINV-PO",
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = poId
        });
        ctx.SaveChanges();
        var sut = new SupplierInvoiceRepository(ctx);

        var result = await sut.GetByPurchaseOrderIdAsync(poId);

        result.Should().ContainSingle(i => i.PurchaseOrderId == poId);
    }
}