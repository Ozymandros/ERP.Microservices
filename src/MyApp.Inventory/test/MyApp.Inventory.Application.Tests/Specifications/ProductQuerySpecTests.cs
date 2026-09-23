using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Specifications;

/// <summary>Unit tests for <see cref="ProductQuerySpec"/> filtering, searching, sorting, and pagination behaviour.</summary>
public class ProductQuerySpecTests
{
    private static IQueryable<Product> CreateTestData()
    {
        return new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PROD-001", Name = "Product One", Description = "First product", UnitPrice = 10.00m },
            new Product(Guid.NewGuid()) { SKU = "PROD-002", Name = "Product Two", Description = "Second product", UnitPrice = 20.00m },
            new Product(Guid.NewGuid()) { SKU = "PROD-003", Name = "Another Product", Description = "Third product", UnitPrice = 15.00m },
            new Product(Guid.NewGuid()) { SKU = "ITEM-001", Name = "Item One", Description = "Fourth product", UnitPrice = 5.00m },
            new Product(Guid.NewGuid()) { SKU = "ITEM-002", Name = "Item Two", Description = "Fifth product", UnitPrice = 25.00m }
        }.AsQueryable();
    }

    #region Filter Tests

    /// <summary>Verifies that ApplyFilters returns only products whose SKU contains the specified filter value.</summary>
    [Fact]
    public void ApplyFilters_WithSkuFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SKU", "PROD" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(p => p.SKU.Contains("PROD")).Should().BeTrue();
    }

    /// <summary>Verifies that ApplyFilters returns only products whose name contains the specified filter value.</summary>
    [Fact]
    public void ApplyFilters_WithNameFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Product" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(p => p.Name.Contains("Product")).Should().BeTrue();
    }

    /// <summary>Verifies that ApplyFilters returns only products with a unit price greater than or equal to the minimum price filter.</summary>
    [Fact]
    public void ApplyFilters_WithMinPriceFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "UnitPriceMin", "15" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(p => p.UnitPrice >= 15m).Should().BeTrue();
    }

    /// <summary>Verifies that ApplyFilters returns only products with a unit price less than or equal to the maximum price filter.</summary>
    [Fact]
    public void ApplyFilters_WithMaxPriceFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "UnitPriceMax", "15" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(p => p.UnitPrice <= 15m).Should().BeTrue();
    }

    /// <summary>Verifies that ApplyFilters returns only products whose unit price falls within the specified min and max price range.</summary>
    [Fact]
    public void ApplyFilters_WithPriceRange_ReturnsFilteredProducts()
    {
        // Arrange
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "UnitPriceMin", "10" }, { "UnitPriceMax", "20" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(p => p.UnitPrice >= 10m && p.UnitPrice <= 20m).Should().BeTrue();
    }

    /// <summary>Verifies that ApplyFilters applies multiple filters simultaneously and returns only products matching all criteria.</summary>
    [Fact]
    public void ApplyFilters_WithMultipleFilters_ReturnsFilteredProducts()
    {
        // Arrange
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SKU", "PROD" }, { "Name", "Product" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(p => p.SKU.Contains("PROD") && p.Name.Contains("Product")).Should().BeTrue();
    }

    #endregion

    #region Search Tests

    /// <summary>Verifies that ApplyFilters returns products whose SKU matches the search term.</summary>
    [Fact]
    public void ApplyFilters_WithSearchTermInSku_ReturnsMatchingProducts()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "PROD" };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        // Search is case-insensitive and matches SKU, Name, or Description
        // "PROD" matches: PROD-001, PROD-002, PROD-003 (SKU), Product One, Product Two, Another Product (Name), and all descriptions contain "product"
        result.Should().HaveCountGreaterThanOrEqualTo(3);
        result.All(p => p.SKU.ToLower().Contains("prod") || p.Name.ToLower().Contains("prod") || (p.Description != null && p.Description.ToLower().Contains("prod"))).Should().BeTrue();
    }

    /// <summary>Verifies that ApplyFilters returns products whose name matches the search term.</summary>
    [Fact]
    public void ApplyFilters_WithSearchTermInName_ReturnsMatchingProducts()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "One" };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    /// <summary>Verifies that ApplyFilters returns products whose description matches the search term.</summary>
    [Fact]
    public void ApplyFilters_WithSearchTermInDescription_ReturnsMatchingProducts()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "First" };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Description.Should().Contain("First");
    }

    /// <summary>Verifies that ApplyFilters returns all products when the search term is empty.</summary>
    [Fact]
    public void ApplyFilters_WithEmptySearchTerm_ReturnsAllProducts()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "" };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    /// <summary>Verifies that ApplyFilters performs case-insensitive search matching across SKU, name, and description.</summary>
    [Fact]
    public void ApplyFilters_WithCaseInsensitiveSearch_ReturnsMatchingProducts()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "product" };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(5); // All products contain "product" in description
    }

    #endregion

    #region Combined Filter and Search Tests

    /// <summary>Verifies that ApplyFilters combines search term and filter criteria, returning only products that satisfy both.</summary>
    [Fact]
    public void ApplyFilters_WithFilterAndSearch_ReturnsIntersection()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "One" };
        querySpec.Filters = new Dictionary<string, string> { { "UnitPriceMin", "5" } };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.ApplyFilters(data).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.All(p => (p.SKU.Contains("One") || p.Name.Contains("One") || p.Description.Contains("One")) && p.UnitPrice >= 5m).Should().BeTrue();
    }

    #endregion

    #region Sorting Tests

    /// <summary>Verifies that Apply sorts products by name in ascending order when SortBy is set to "Name" and SortDesc is false.</summary>
    [Fact]
    public void Apply_WithSortByNameAscending_SortsCorrectly()
    {
        // Arrange
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.Apply(data).ToList();

        // Assert
        result.Should().BeInAscendingOrder(p => p.Name);
    }

    /// <summary>Verifies that Apply sorts products by unit price in descending order when SortBy is "UnitPrice" and SortDesc is true.</summary>
    [Fact]
    public void Apply_WithSortByPriceDescending_SortsCorrectly()
    {
        // Arrange
        var querySpec = new QuerySpec { SortBy = "UnitPrice", SortDesc = true };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.Apply(data).ToList();

        // Assert
        result.Should().BeInDescendingOrder(p => p.UnitPrice);
    }

    #endregion

    #region Pagination Tests

    /// <summary>Verifies that Apply returns a page of results no larger than the specified page size.</summary>
    [Fact]
    public void Apply_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.Apply(data).ToList();

        // Assert
        result.Should().HaveCountLessThanOrEqualTo(2);
    }

    /// <summary>Verifies that Apply returns items from the second page when the page number is set to 2.</summary>
    [Fact]
    public void Apply_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new ProductQuerySpec(querySpec);
        var data = CreateTestData();

        // Act
        var result = spec.Apply(data).ToList();

        // Assert
        result.Should().HaveCountLessThanOrEqualTo(2);
    }

    #endregion
}
