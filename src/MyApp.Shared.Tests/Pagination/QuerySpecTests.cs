using FluentAssertions;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Shared.Tests.Pagination;

public class QuerySpecTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultValues_SetsDefaults()
    {
        // Act
        var querySpec = new QuerySpec();

        // Assert
        querySpec.Page.Should().Be(1);
        querySpec.PageSize.Should().Be(20);
        querySpec.SortBy.Should().BeNull();
        querySpec.SortDesc.Should().BeFalse();
        querySpec.Filters.Should().BeNull();
        querySpec.SearchTerm.Should().BeNull();
        querySpec.SearchFields.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithValidPageAndPageSize_SetsValues()
    {
        // Act
        var querySpec = new QuerySpec(2, 50);

        // Assert
        querySpec.Page.Should().Be(2);
        querySpec.PageSize.Should().Be(50);
    }

    [Fact]
    public void Constructor_WithZeroPage_NormalizesToPageOne()
    {
        // Act
        var querySpec = new QuerySpec(0, 20);

        // Assert
        querySpec.Page.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNegativePage_NormalizesToPageOne()
    {
        // Act
        var querySpec = new QuerySpec(-1, 20);

        // Assert
        querySpec.Page.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithZeroPageSize_NormalizesToDefault()
    {
        // Act
        var querySpec = new QuerySpec(1, 0);

        // Assert
        querySpec.PageSize.Should().Be(20);
    }

    [Fact]
    public void Constructor_WithNegativePageSize_NormalizesToDefault()
    {
        // Act
        var querySpec = new QuerySpec(1, -1);

        // Assert
        querySpec.PageSize.Should().Be(20);
    }

    [Fact]
    public void Constructor_WithPageSizeOver100_NormalizesTo100()
    {
        // Act
        var querySpec = new QuerySpec(1, 150);

        // Assert
        querySpec.PageSize.Should().Be(100);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithValidValues_DoesNotChangeValues()
    {
        // Arrange
        var querySpec = new QuerySpec
        {
            Page = 2,
            PageSize = 50,
            SortBy = "Name",
            SortDesc = true
        };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.Page.Should().Be(2);
        querySpec.PageSize.Should().Be(50);
        querySpec.SortBy.Should().Be("Name");
        querySpec.SortDesc.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroPage_NormalizesToPageOne()
    {
        // Arrange
        var querySpec = new QuerySpec { Page = 0 };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.Page.Should().Be(1);
    }

    [Fact]
    public void Validate_WithNegativePage_NormalizesToPageOne()
    {
        // Arrange
        var querySpec = new QuerySpec { Page = -5 };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.Page.Should().Be(1);
    }

    [Fact]
    public void Validate_WithZeroPageSize_NormalizesToDefault()
    {
        // Arrange
        var querySpec = new QuerySpec { PageSize = 0 };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.PageSize.Should().Be(20);
    }

    [Fact]
    public void Validate_WithNegativePageSize_NormalizesToDefault()
    {
        // Arrange
        var querySpec = new QuerySpec { PageSize = -10 };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.PageSize.Should().Be(20);
    }

    [Fact]
    public void Validate_WithPageSizeOver100_NormalizesTo100()
    {
        // Arrange
        var querySpec = new QuerySpec { PageSize = 200 };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.PageSize.Should().Be(100);
    }

    [Fact]
    public void Validate_WithPageSizeExactly100_KeepsValue()
    {
        // Arrange
        var querySpec = new QuerySpec { PageSize = 100 };

        // Act
        querySpec.Validate();

        // Assert
        querySpec.PageSize.Should().Be(100);
    }

    #endregion

    #region Filters Tests

    [Fact]
    public void Filters_CanBeSetAndRetrieved()
    {
        // Arrange
        var querySpec = new QuerySpec();
        var filters = new Dictionary<string, string>
        {
            { "status", "active" },
            { "category", "electronics" }
        };

        // Act
        querySpec.Filters = filters;

        // Assert
        querySpec.Filters.Should().NotBeNull();
        querySpec.Filters.Should().HaveCount(2);
        querySpec.Filters["status"].Should().Be("active");
        querySpec.Filters["category"].Should().Be("electronics");
    }

    [Fact]
    public void Filters_CanBeNull()
    {
        // Arrange
        var querySpec = new QuerySpec { Filters = new Dictionary<string, string>() };

        // Act
        querySpec.Filters = null;

        // Assert
        querySpec.Filters.Should().BeNull();
    }

    #endregion

    #region SearchTerm Tests

    [Fact]
    public void SearchTerm_CanBeSetAndRetrieved()
    {
        // Arrange
        var querySpec = new QuerySpec();

        // Act
        querySpec.SearchTerm = "test search";

        // Assert
        querySpec.SearchTerm.Should().Be("test search");
    }

    [Fact]
    public void SearchTerm_CanBeNull()
    {
        // Arrange
        var querySpec = new QuerySpec { SearchTerm = "test" };

        // Act
        querySpec.SearchTerm = null;

        // Assert
        querySpec.SearchTerm.Should().BeNull();
    }

    #endregion

    #region SearchFields Tests

    [Fact]
    public void SearchFields_CanBeSetAndRetrieved()
    {
        // Arrange
        var querySpec = new QuerySpec();

        // Act
        querySpec.SearchFields = "name,description";

        // Assert
        querySpec.SearchFields.Should().Be("name,description");
    }

    #endregion

    #region SortBy Tests

    [Fact]
    public void SortBy_CanBeSetAndRetrieved()
    {
        // Arrange
        var querySpec = new QuerySpec();

        // Act
        querySpec.SortBy = "Name";

        // Assert
        querySpec.SortBy.Should().Be("Name");
    }

    [Fact]
    public void SortBy_CanBeNull()
    {
        // Arrange
        var querySpec = new QuerySpec { SortBy = "Name" };

        // Act
        querySpec.SortBy = null;

        // Assert
        querySpec.SortBy.Should().BeNull();
    }

    #endregion

    #region SortDesc Tests

    [Fact]
    public void SortDesc_DefaultsToFalse()
    {
        // Arrange & Act
        var querySpec = new QuerySpec();

        // Assert
        querySpec.SortDesc.Should().BeFalse();
    }

    [Fact]
    public void SortDesc_CanBeSetToTrue()
    {
        // Arrange
        var querySpec = new QuerySpec();

        // Act
        querySpec.SortDesc = true;

        // Assert
        querySpec.SortDesc.Should().BeTrue();
    }

    #endregion
}
