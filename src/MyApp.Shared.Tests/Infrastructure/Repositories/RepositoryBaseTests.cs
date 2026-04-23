using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Infrastructure.Repositories;
using System.Linq;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Repositories;

// Test entity for Repository base class testing
public class RepositoryTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Test DbContext for Repository base class testing
public class RepositoryTestDbContext : DbContext
{
    public RepositoryTestDbContext(DbContextOptions<RepositoryTestDbContext> options) : base(options) { }

    public DbSet<RepositoryTestEntity> TestEntities => Set<RepositoryTestEntity>();
}

// Concrete implementation of Repository for testing
public class TestRepository : Repository<RepositoryTestEntity, Guid>
{
    public TestRepository(DbContext dbContext) : base(dbContext) { }
}

// Test specification for QueryAsync testing
public class RepositoryTestEntitySpecification : BaseSpecification<RepositoryTestEntity>
{
    public RepositoryTestEntitySpecification(QuerySpec query) : base(query) { }

    public override IQueryable<RepositoryTestEntity> ApplyFilters(IQueryable<RepositoryTestEntity> query)
    {
        if (Query.Filters != null)
        {
            if (Query.Filters.TryGetValue("name", out var nameValue))
            {
                query = query.Where(e => e.Name.Contains(nameValue));
            }
            if (Query.Filters.TryGetValue("value", out var valueValue) && int.TryParse(valueValue, out var value))
            {
                query = query.Where(e => e.Value == value);
            }
        }

        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term));
        }

        return query;
    }
}

public class RepositoryBaseTests : IDisposable
{
    private readonly RepositoryTestDbContext _context;
    private readonly TestRepository _repository;

    public RepositoryBaseTests()
    {
        var options = new DbContextOptionsBuilder<RepositoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new RepositoryTestDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new TestRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var entity = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Test", Value = 10 };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ReturnsNull()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        var result = await _repository.GetByIdAsync(emptyId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 1", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 2", Value = 20 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 3", Value = 30 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(e => e.Name == "Entity 1");
        result.Should().Contain(e => e.Name == "Entity 2");
        result.Should().Contain(e => e.Name == "Entity 3");
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyDatabase_ReturnsEmptyCollection()
    {
        // Arrange - no entities added

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(2, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllPaginatedAsync_WithLastPage_ReturnsRemainingItems()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(3, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5); // Remaining 5 items
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllPaginatedAsync_WithPageBeyondTotal_ReturnsEmptyPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 5).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(2, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllPaginatedAsync_WithPageSizeZero_NormalizesToDefaultPageSize()
    {
        // Arrange
        var entities = Enumerable.Range(1, 5).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(1, 0);

        // Assert
        result.Should().NotBeNull();
        // PaginationParams normalizes pageSize 0 to 10
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(5); // All 5 items fit in page 1
        result.TotalCount.Should().Be(5);
    }

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Banana", Value = 20 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple Pie", Value = 30 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "name", "Apple" } }
        };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(e => e.Name.Contains("Apple"));
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Banana", Value = 20 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple Pie", Value = 30 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec { SearchTerm = "Apple" };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(e => e.Name.Contains("Apple"));
    }

    [Fact]
    public async Task QueryAsync_WithSorting_ReturnsSortedResults()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Charlie", Value = 30 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Alpha", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Bravo", Value = 20 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec
        {
            SortBy = "name",
            SortDesc = false
        };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.Select(e => e.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task QueryAsync_WithDescendingSort_ReturnsDescendingSortedResults()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Alpha", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Bravo", Value = 20 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Charlie", Value = 30 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec
        {
            SortBy = "name",
            SortDesc = true
        };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.Select(e => e.Name).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec
        {
            Page = 2,
            PageSize = 10
        };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task QueryAsync_WithCombinedFiltersAndSorting_ReturnsCorrectResults()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple", Value = 30 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple Pie", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Banana", Value = 20 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "name", "Apple" } },
            SortBy = "value",
            SortDesc = false
        };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(e => e.Name.Contains("Apple"));
        result.Items.Select(e => e.Value).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task QueryAsync_WithNoMatches_ReturnsEmptyResult()
    {
        // Arrange
        var entities = new[]
        {
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Apple", Value = 10 },
            new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Banana", Value = 20 }
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        var query = new QuerySpec
        {
            Filters = new Dictionary<string, string> { { "name", "Cherry" } }
        };
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task QueryAsync_UsesAsNoTracking()
    {
        // Arrange
        var entity = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Test", Value = 10 };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        var query = new QuerySpec();
        var spec = new RepositoryTestEntitySpecification(query);

        // Act
        var result = await _repository.QueryAsync(spec);
        var retrievedEntity = result.Items.First();

        // Modify the entity
        retrievedEntity.Name = "Modified";

        // Verify it's not tracked
        var trackedEntity = _context.Entry(retrievedEntity);
        trackedEntity.State.Should().Be(EntityState.Detached);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidEntity_AddsEntityToDatabase()
    {
        // Arrange
        var entity = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "New Entity", Value = 100 };

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);

        var savedEntity = await _context.TestEntities.FindAsync(entity.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Name.Should().Be("New Entity");
    }

    [Fact]
    public async Task AddAsync_WithMultipleEntities_AddsAllToDatabase()
    {
        // Arrange
        var entity1 = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 1", Value = 10 };
        var entity2 = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 2", Value = 20 };

        // Act
        await _repository.AddAsync(entity1);
        await _repository.AddAsync(entity2);

        // Assert
        var allEntities = await _repository.GetAllAsync();
        allEntities.Should().HaveCount(2);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_UpdatesEntityInDatabase()
    {
        // Arrange
        var entity = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Original", Value = 10 };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        entity.Name = "Updated";
        entity.Value = 20;

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated");
        result.Value.Should().Be(20);

        var updatedEntity = await _context.TestEntities.FindAsync(entity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Name.Should().Be("Updated");
        updatedEntity.Value.Should().Be(20);
    }

    [Fact]
    public async Task UpdateAsync_WithNonTrackedEntity_UpdatesEntity()
    {
        // Arrange
        var entity = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Original", Value = 10 };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached; // Detach to simulate non-tracked

        entity.Name = "Updated";
        entity.Value = 20;

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated");

        var updatedEntity = await _context.TestEntities.FindAsync(entity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Name.Should().Be("Updated");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_RemovesEntityFromDatabase()
    {
        // Arrange
        var entity = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "To Delete", Value = 10 };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity);

        // Assert
        var deletedEntity = await _context.TestEntities.FindAsync(entity.Id);
        deletedEntity.Should().BeNull();

        var allEntities = await _repository.GetAllAsync();
        allEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithMultipleEntities_RemovesOnlySpecifiedEntity()
    {
        // Arrange
        var entity1 = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 1", Value = 10 };
        var entity2 = new RepositoryTestEntity { Id = Guid.NewGuid(), Name = "Entity 2", Value = 20 };
        _context.TestEntities.AddRange(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity1);

        // Assert
        var remainingEntities = await _repository.GetAllAsync();
        remainingEntities.Should().HaveCount(1);
        remainingEntities.Should().Contain(e => e.Id == entity2.Id);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task GetAllPaginatedAsync_WithNegativePageNumber_NormalizesToPageOne()
    {
        // Arrange
        var entities = Enumerable.Range(1, 5).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(-1, 10);

        // Assert
        result.Should().NotBeNull();
        // PaginationParams normalizes negative pageNumber to 1
        result.PageNumber.Should().Be(1);
        result.Items.Should().HaveCount(5); // All items on page 1
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetAllPaginatedAsync_WithLargePageSize_ReturnsAllItems()
    {
        // Arrange
        var entities = Enumerable.Range(1, 5).Select(i => new RepositoryTestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Value = i
        }).ToArray();
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPaginatedAsync(1, 100);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task QueryAsync_WithNullSpecification_ThrowsException()
    {
        // Arrange
        ISpecification<RepositoryTestEntity>? spec = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.QueryAsync(spec!));
    }

    #endregion
}
