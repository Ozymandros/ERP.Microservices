using FluentAssertions;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Sales.Application.Tests.Specifications;

public class CustomerQuerySpecTests
{
    private IQueryable<Customer> CreateTestData()
    {
        return new List<Customer>
        {
            new Customer(Guid.NewGuid()) { Name = "Customer One", Email = "one@example.com", PhoneNumber = "111-111-1111", Address = "123 Main St" },
            new Customer(Guid.NewGuid()) { Name = "Customer Two", Email = "two@example.com", PhoneNumber = "222-222-2222", Address = "456 Oak Ave" },
            new Customer(Guid.NewGuid()) { Name = "Client Three", Email = "three@example.com", PhoneNumber = "333-333-3333", Address = "789 Pine Rd" }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithNameFilter_ReturnsFilteredCustomers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Customer" } };
        var spec = new CustomerQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyFilters_WithEmailFilter_ReturnsFilteredCustomers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Email", "one" } };
        var spec = new CustomerQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().Email.Should().Contain("one");
    }

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingCustomers()
    {
        var querySpec = new QuerySpec { SearchTerm = "Customer" };
        var spec = new CustomerQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WithSortByName_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new CustomerQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(c => c.Name);
    }
}
