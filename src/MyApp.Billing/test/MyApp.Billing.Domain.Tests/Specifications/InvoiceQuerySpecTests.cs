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
}