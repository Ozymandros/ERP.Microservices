using FluentAssertions;
using Xunit;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Billing.Domain.Tests.Specifications;

public class InvoiceQuerySpecTests
{
    private static IQueryable<Invoice> SampleInvoices()
    {
        var customer = Guid.NewGuid();
        var draft = new Invoice(Guid.NewGuid(), "INV-DRAFT", customer, "USD");
        draft.AddLine("A", 1, 10m, 0m);
        var issued = new Invoice(Guid.NewGuid(), "INV-OTHER", customer, "eur");
        issued.AddLine("B", 1, 5m, 0m);
        issued.Issue("INV-100", new DateTime(2026, 1, 10), 30);
        return new List<Invoice> { draft, issued }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_Status_FiltersDraft()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Invoice.Status), "Draft" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-DRAFT");
    }

    [Fact]
    public void ApplyFilters_Currency_NormalizesCase()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Invoice.Currency), "eur" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-OTHER");
    }

    [Fact]
    public void ApplyFilters_IssueDateFrom_FiltersIssued()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "IssueDateFrom", "2026-01-01" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-OTHER");
    }

    [Fact]
    public void ApplyFilters_SearchTerm_MatchesInvoiceNumber()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec { SearchTerm = "DRAFT" });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-DRAFT");
    }

    [Fact]
    public void ApplyFilters_CustomerId_Filters()
    {
        var invoices = SampleInvoices();
        var customerId = invoices.First(i => i.InvoiceNumber == "INV-DRAFT").CustomerId;
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Invoice.CustomerId), customerId.ToString() } }
        });

        var result = spec.ApplyFilters(invoices).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.CustomerId == customerId);
    }

    [Fact]
    public void ApplyFilters_IssueDateTo_ExcludesLaterIssued()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "IssueDateTo", "2026-01-05" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplyFilters_TotalGrossMin_FiltersIssued()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "TotalGrossMin", "5" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-OTHER");
    }

    [Fact]
    public void ApplyFilters_OrderId_Filters()
    {
        var invoices = SampleInvoices();
        var orderId = Guid.NewGuid();
        invoices.First(i => i.InvoiceNumber == "INV-OTHER").LinkToSalesOrder(orderId);

        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Invoice.OrderId), orderId.ToString() } }
        });

        var result = spec.ApplyFilters(invoices).ToList();

        result.Should().ContainSingle(i => i.OrderId == orderId);
    }

    [Fact]
    public void ApplyFilters_DueDateFrom_FiltersIssued()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "DueDateFrom", "2026-02-01" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-OTHER");
    }

    [Fact]
    public void ApplyFilters_OutstandingAmountMax_FiltersDraft()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "OutstandingAmountMax", "200" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().Contain(i => i.InvoiceNumber == "INV-DRAFT");
    }

    [Fact]
    public void ApplyFilters_InvoiceNumber_ContainsMatch()
    {
        var spec = new InvoiceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Invoice.InvoiceNumber), "inv-other" } }
        });

        var result = spec.ApplyFilters(SampleInvoices()).ToList();

        result.Should().ContainSingle(i => i.InvoiceNumber == "INV-OTHER");
    }
}