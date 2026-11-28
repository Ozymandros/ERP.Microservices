using FluentAssertions;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Billing.Infrastructure.Tests.Helpers;
using Xunit;

namespace MyApp.Billing.Infrastructure.Tests.Persistence;

public class BillingDbContextSaveChangesTests
{
    [Fact]
    public void SaveChanges_NewPaymentOnTrackedInvoice_MarksPaymentAdded()
    {
        using var ctx = TestDbContextFactory.Create();
        var invoice = new Invoice(Guid.NewGuid(), "INV-PAY", Guid.NewGuid(), "USD");
        invoice.AddLine("Widget", 1, 100m, 0m);
        invoice.Issue("INV-PAY", DateTime.UtcNow, 30);
        ctx.Invoices.Add(invoice);
        ctx.SaveChanges();

        var payment = new Payment(Guid.NewGuid(), invoice.Id, 100m, "USD", "Wire", DateTime.UtcNow);
        invoice.Payments.Add(payment);
        ctx.Invoices.Update(invoice);

        ctx.SaveChanges();

        ctx.Payments.Should().ContainSingle(p => p.Id == payment.Id);
    }
}