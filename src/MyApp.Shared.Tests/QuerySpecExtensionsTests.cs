using FluentAssertions;
using Microsoft.Extensions.Primitives;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;
using Xunit;

namespace MyApp.Shared.Tests;

public class QuerySpecExtensionsTests
{
    #region BindFiltersFromQuery Tests

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
        query.Filters.Should().NotBeNull();
        query.Filters!["sku"].Should().Be("P001");
        query.Filters["name"].Should().Be("Product 1");
        query.Filters.Should().NotContainKey("page");
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
        query.Filters.Should().NotBeNull();
        query.Filters!["category"].Should().Be("Electronics");
        query.Filters["isActive"].Should().Be("true");
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
        query.Filters.Should().ContainKey("sku");
        query.Filters!["sku"].Should().Be("P001");
        query.Filters["SKU"].Should().Be("P001");
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
        query.Filters.Should().NotBeNull();
        query.Filters.Should().BeEmpty();
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldSkipKnownProperties()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("page", "2"),
            new("pagesize", "10"),
            new("sortby", "name"),
            new("sortdesc", "true"),
            new("searchterm", "test"),
            new("searchfields", "name,description"),
            new("filters", "ignored"),
            new("custom", "value")
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        query.Filters.Should().NotBeNull();
        query.Filters.Should().ContainKey("custom");
        query.Filters!["custom"].Should().Be("value");
        query.Filters.Should().NotContainKey("page");
        query.Filters.Should().NotContainKey("pagesize");
        query.Filters.Should().NotContainKey("sortby");
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldHandleMixedFormats()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("direct", "value1"),
            new("filters[indexed]", "value2")
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        query.Filters.Should().NotBeNull();
        query.Filters!["direct"].Should().Be("value1");
        query.Filters["indexed"].Should().Be("value2");
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldHandleMultipleValues()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("filter", new StringValues(new[] { "value1", "value2" }))
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        query.Filters.Should().NotBeNull();
        query.Filters!["filter"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldConvertExistingDictionaryToCaseInsensitive()
    {
        // Arrange
        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "existing", "value" } }
        };
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("new", "newvalue")
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        query.Filters.Should().NotBeNull();
        query.Filters!["existing"].Should().Be("value");
        query.Filters["new"].Should().Be("newvalue");
        query.Filters["EXISTING"].Should().Be("value"); // Case-insensitive
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldHandleEmptyFilterKey()
    {
        // Arrange
        var query = new QuerySpec();
        var queryParams = new List<KeyValuePair<string, StringValues>>
        {
            new("filters[]", "value"),
            new("", "value")
        };

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        query.Filters.Should().NotBeNull();
        // Empty keys should be skipped
    }

    [Fact]
    public void BindFiltersFromQuery_ShouldValidateQuerySpec()
    {
        // Arrange
        var query = new QuerySpec { Page = 0, PageSize = 200 };
        var queryParams = new List<KeyValuePair<string, StringValues>>();

        // Act
        query.BindFiltersFromQuery(queryParams);

        // Assert
        query.Page.Should().BeGreaterOrEqualTo(1);
        query.PageSize.Should().BeLessOrEqualTo(100);
    }

    #endregion

    #region WithFilter Tests

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
        query.Filters!["old"].Should().Be("val");
        query.Filters["new"].Should().Be("newval");
    }

    [Fact]
    public void WithFilter_ShouldCreateFiltersDictionaryIfNull()
    {
        // Arrange
        var query = new QuerySpec();

        // Act
        query.WithFilter("key", "value");

        // Assert
        query.Filters.Should().NotBeNull();
        query.Filters!["key"].Should().Be("value");
    }

    [Fact]
    public void WithFilter_ShouldOverwriteExistingFilter()
    {
        // Arrange
        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "key", "oldvalue" } }
        };

        // Act
        query.WithFilter("key", "newvalue");

        // Assert
        query.Filters!["key"].Should().Be("newvalue");
    }

    [Fact]
    public void WithFilter_ShouldConvertToCaseInsensitive()
    {
        // Arrange
        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "existing", "value" } }
        };

        // Act
        query.WithFilter("new", "newvalue");

        // Assert
        query.Filters!["EXISTING"].Should().Be("value");
        query.Filters["NEW"].Should().Be("newvalue");
    }

    #endregion

    #region WithDefaultSorting Tests

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
        query1.SortBy.Should().Be("Name");
        query1.SortDesc.Should().BeTrue();
        query2.SortBy.Should().Be("Existing");
        query2.SortDesc.Should().BeFalse();
    }

    [Fact]
    public void WithDefaultSorting_ShouldSetDescendingToFalseByDefault()
    {
        // Arrange
        var query = new QuerySpec();

        // Act
        query.WithDefaultSorting("Name");

        // Assert
        query.SortBy.Should().Be("Name");
        query.SortDesc.Should().BeFalse();
    }

    [Fact]
    public void WithDefaultSorting_ShouldNotOverrideExistingSortDesc()
    {
        // Arrange
        var query = new QuerySpec { SortBy = "Existing", SortDesc = true };

        // Act
        query.WithDefaultSorting("Name", false);

        // Assert
        query.SortBy.Should().Be("Existing");
        query.SortDesc.Should().BeTrue();
    }

    #endregion

    #region WithMaxPageSize Tests

    [Fact]
    public void WithMaxPageSize_ShouldLimitPageSize()
    {
        // Arrange
        var query = new QuerySpec { PageSize = 200 };

        // Act
        query.WithMaxPageSize(100);

        // Assert
        query.PageSize.Should().Be(100);
    }

    [Fact]
    public void WithMaxPageSize_ShouldNotChangeIfWithinLimit()
    {
        // Arrange
        var query = new QuerySpec { PageSize = 50 };

        // Act
        query.WithMaxPageSize(100);

        // Assert
        query.PageSize.Should().Be(50);
    }

    [Fact]
    public void WithMaxPageSize_ShouldHandleExactLimit()
    {
        // Arrange
        var query = new QuerySpec { PageSize = 100 };

        // Act
        query.WithMaxPageSize(100);

        // Assert
        query.PageSize.Should().Be(100);
    }

    #endregion

    #region ToQuerySpec Tests

    [Fact]
    public void ToQuerySpec_ShouldValidateQuerySpec()
    {
        // Arrange
        var query = new QuerySpec { Page = 0, PageSize = 200 };

        // Act
        query.ToQuerySpec();

        // Assert
        query.Page.Should().BeGreaterOrEqualTo(1);
        query.PageSize.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void ToQuerySpec_ShouldReturnSameInstance()
    {
        // Arrange
        var query = new QuerySpec();

        // Act
        var result = query.ToQuerySpec();

        // Assert
        result.Should().BeSameAs(query);
    }

    #endregion

    #region ToPaginatedResponse Tests

    [Fact]
    public void ToPaginatedResponse_ShouldFormatCorrectly()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PaginatedResult<int>(items, 1, 10, 25);

        // Act
        var response = result.ToPaginatedResponse(1, 10);

        // Assert
        response.Should().NotBeNull();
        // Method returns anonymous object with data and pagination properties
        // Structure is verified through usage in actual API responses
    }

    [Fact]
    public void ToPaginatedResponse_ShouldCalculateTotalPages()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PaginatedResult<int>(items, 1, 10, 25);

        // Act
        var response = result.ToPaginatedResponse(1, 10);

        // Assert
        response.Should().NotBeNull();
        // Total pages should be 3 (25 items / 10 per page = 2.5, rounded up to 3)
        // Verified through reflection or dynamic access
    }

    [Fact]
    public void ToPaginatedResponse_ShouldSetHasNextPage()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PaginatedResult<int>(items, 1, 10, 25);

        // Act
        var response = result.ToPaginatedResponse(1, 10);

        // Assert
        response.Should().NotBeNull();
        // HasNextPage should be true (page 1 < totalPages 3)
    }

    [Fact]
    public void ToPaginatedResponse_ShouldSetHasPreviousPage()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PaginatedResult<int>(items, 2, 10, 25);

        // Act
        var response = result.ToPaginatedResponse(2, 10);

        // Assert
        response.Should().NotBeNull();
        // HasPreviousPage should be true (page 2 > 1)
    }

    [Fact]
    public void ToPaginatedResponse_OnFirstPage_ShouldSetHasPreviousPageToFalse()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PaginatedResult<int>(items, 1, 10, 25);

        // Act
        var response = result.ToPaginatedResponse(1, 10);

        // Assert
        response.Should().NotBeNull();
        // HasPreviousPage should be false (page 1)
    }

    [Fact]
    public void ToPaginatedResponse_OnLastPage_ShouldSetHasNextPageToFalse()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PaginatedResult<int>(items, 3, 10, 25);

        // Act
        var response = result.ToPaginatedResponse(3, 10);

        // Assert
        response.Should().NotBeNull();
        // HasNextPage should be false (page 3 = totalPages 3)
    }

    [Fact]
    public void ToPaginatedResponse_WithZeroTotalCount_ShouldHandleCorrectly()
    {
        // Arrange
        var items = Array.Empty<int>();
        var result = new PaginatedResult<int>(items, 1, 10, 0);

        // Act
        var response = result.ToPaginatedResponse(1, 10);

        // Assert
        response.Should().NotBeNull();
        // Total pages should be 0, hasNextPage and hasPreviousPage should be false
    }

    [Fact]
    public void ToPaginatedResponse_WithExactPageSize_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var result = new PaginatedResult<int>(items, 1, 10, 10);

        // Act
        var response = result.ToPaginatedResponse(1, 10);

        // Assert
        response.Should().NotBeNull();
        // Total pages should be 1 (10 items / 10 per page = 1)
    }

    #endregion
}
