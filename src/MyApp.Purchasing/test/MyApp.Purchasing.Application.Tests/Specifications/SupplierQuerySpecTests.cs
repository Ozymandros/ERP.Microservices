using FluentAssertions;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Specifications;

public class SupplierQuerySpecTests
{
    private static IQueryable<Supplier> CreateTestData()
    {
        return new List<Supplier>
        {
            new Supplier(Guid.NewGuid()) { Name = "Supplier One", Email = "one@supplier.com", ContactName = "John Doe", PhoneNumber = "111-111-1111", Address = "123 Main St" },
            new Supplier(Guid.NewGuid()) { Name = "Supplier Two", Email = "two@supplier.com", ContactName = "Jane Smith", PhoneNumber = "222-222-2222", Address = "456 Oak Ave" },
            new Supplier(Guid.NewGuid()) { Name = "Vendor Three", Email = "three@supplier.com", ContactName = "Bob Johnson", PhoneNumber = "333-333-3333", Address = "789 Pine Rd" }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithNameFilter_ReturnsFilteredSuppliers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Supplier" } };
        var spec = new SupplierQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyFilters_WithEmailFilter_ReturnsFilteredSuppliers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Email", "one" } };
        var spec = new SupplierQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyFilters_WithContactNameFilter_ReturnsFilteredSuppliers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "ContactName", "Doe" } }; // More specific to avoid matching "Johnson"
        var spec = new SupplierQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().ContactName.Should().Contain("Doe");
    }

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingSuppliers()
    {
        var querySpec = new QuerySpec { SearchTerm = "Supplier" };
        var spec = new SupplierQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        // Search matches Name, Email, ContactName, PhoneNumber, Address
        // "Supplier" matches: Supplier One (name), Supplier Two (name), and all emails contain "@supplier.com"
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.All(s => s.Name.ToLower().Contains("supplier") || s.Email.ToLower().Contains("supplier") || 
                       (s.ContactName != null && s.ContactName.ToLower().Contains("supplier")) ||
                       (s.PhoneNumber != null && s.PhoneNumber.ToLower().Contains("supplier")) ||
                       (s.Address != null && s.Address.ToLower().Contains("supplier"))).Should().BeTrue();
    }

    [Fact]
    public void Apply_WithSortByName_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new SupplierQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(s => s.Name);
    }
}
