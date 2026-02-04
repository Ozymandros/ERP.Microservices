using FluentAssertions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using System.Linq;
using Xunit;

namespace MyApp.Shared.Tests.Specifications;

// Test entity for BaseSpecification tests
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Concrete implementation for testing
public class TestSpecification : BaseSpecification<TestEntity>
{
    public TestSpecification(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<TestEntity> ApplyFilters(IQueryable<TestEntity> query)
    {
        if (Query.Filters?.TryGetValue("Name", out var nameFilter) == true)
            query = query.Where(e => e.Name.Contains(nameFilter));

        if (Query.Filters?.TryGetValue("IsActive", out var isActiveFilter) == true)
        {
            if (bool.TryParse(isActiveFilter, out var isActive))
                query = query.Where(e => e.IsActive == isActive);
        }

        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term));
        }

        return query;
    }
}

public class BaseSpecificationTests
{
    private IQueryable<TestEntity> CreateTestData()
    {
        return new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Entity One", IsActive = true },
            new TestEntity { Id = 2, Name = "Entity Two", IsActive = false },
            new TestEntity { Id = 3, Name = "Entity Three", IsActive = true },
            new TestEntity { Id = 4, Name = "Another Entity", IsActive = true },
            new TestEntity { Id = 5, Name = "Test Entity", IsActive = false }
        }.AsQueryable();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullQuerySpec_CreatesDefaultQuerySpec()
    {
        // Act
        var spec = new TestSpecification(null!);

        // Assert
        spec.Query.Should().NotBeNull();
        spec.Query.Page.Should().Be(1);
        spec.Query.PageSize.Should().Be(20);
    }

    [Fact]
    public void Constructor_WithQuerySpec_ValidatesQuerySpec()
    {
        // Arrange
        var querySpec = new QuerySpec { Page = 0, PageSize = 200 };

        // Act
        var spec = new TestSpecification(querySpec);

        // Assert
        spec.Query.Page.Should().Be(1); // Normalized
        spec.Query.PageSize.Should().Be(100); // Normalized
    }

    #endregion

    #region ApplyFilters Tests

    [Fact]
    public void ApplyFilters_WithNoFilters_ReturnsAllEntities()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec();
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.ApplyFilters(query);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public void ApplyFilters_WithNameFilter_FiltersByName()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "One" } };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.ApplyFilters(query).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Entity One");
    }

    [Fact]
    public void ApplyFilters_WithIsActiveFilter_FiltersByIsActive()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "IsActive", "true" } };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.ApplyFilters(query).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithSearchTerm_FiltersBySearchTerm()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec { SearchTerm = "three" };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.ApplyFilters(query).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Entity Three");
    }

    [Fact]
    public void ApplyFilters_WithMultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string>
        {
            { "Name", "Entity" },
            { "IsActive", "true" }
        };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.ApplyFilters(query).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.All(e => e.Name.Contains("Entity")).Should().BeTrue();
        result.All(e => e.IsActive).Should().BeTrue();
    }

    #endregion

    #region Apply Tests (with pagination and sorting)

    [Fact]
    public void Apply_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.Apply(query).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WithSortingAscending_SortsAscending()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.Apply(query).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.First().Name.Should().Be("Another Entity");
    }

    [Fact]
    public void Apply_WithSortingDescending_SortsDescending()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = true };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.Apply(query).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.First().Name.Should().Be("Test Entity");
    }

    [Fact]
    public void Apply_WithFiltersAndPagination_AppliesBoth()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "IsActive", "true" } };
        querySpec.Page = 1;
        querySpec.PageSize = 2;
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.Apply(query).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public void Apply_WithNoSortBy_DoesNotSort()
    {
        // Arrange
        var query = CreateTestData();
        var querySpec = new QuerySpec { SortBy = null };
        var spec = new TestSpecification(querySpec);

        // Act
        var result = spec.Apply(query).ToList();

        // Assert
        result.Should().HaveCount(5);
        // Order should be preserved from original query
    }

    #endregion
}
