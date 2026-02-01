using Microsoft.Extensions.Primitives;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;
using Xunit;

namespace MyApp.Shared.Tests;

public class QuerySpecExtensionsTests
{
    [Fact]
    public void BindFiltersFromQuery_ShouldBindDirectFilters()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("sku", "P001"),
            new("name", "Product 1"),
            new("page", "2") // Should be skipped as it's a known property
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        Assert.NotNull(query.Filters);
        Assert.Equal("P001", query.Filters["sku"]);
        Assert.Equal("Product 1", query.Filters["name"]);
        Assert.False(query.Filters.ContainsKey("page"));
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldBindIndexedFilters()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("filters[category]", "Electronics"),
            new("filters[isActive]", "true")
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        Assert.NotNull(query.Filters);
        Assert.Equal("Electronics", query.Filters["category"]);
        Assert.Equal("true", query.Filters["isActive"]);
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldBeCaseInsensitive()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("SKU", "P001")
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        Assert.True(query.Filters.ContainsKey("sku"));
        Assert.Equal("P001", query.Filters["sku"]);
        Assert.Equal("P001", query.Filters["SKU"]);
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldHandleEmptyQuery()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>();

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        Assert.NotNull(query.Filters);
        Assert.Empty(query.Filters);
    }

    [Fact]
    public void WithFilter_ShouldMergeFilters()
    {
        // Arrange
        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "old", "val" } }
        };

        // Act
        query.WithFilter("new", "newval");

        // Assert
        Assert.Equal("val", query.Filters["old"]);
        Assert.Equal("newval", query.Filters["new"]);
    }

    [Fact]
    public void WithDefaultSorting_ShouldApplyOnlyIfMissing()
    {
        // Arrange
        var query1 = new QuerySpec();
        var query2 = new QuerySpec { SortBy = "Existing" };

        // Act
        query1.WithDefaultSorting("Name", true);
        query2.WithDefaultSorting("Name", true);

        // Assert
        Assert.Equal("Name", query1.SortBy);
        Assert.True(query1.SortDesc);
        Assert.Equal("Existing", query2.SortBy);
        Assert.False(query2.SortDesc);
    }
}
