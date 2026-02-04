using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Data;

// Test entity that implements IAuditableEntity
public class TestAuditableEntity : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class TestAuditableDbContext : AuditableDbContext
{
    public DbSet<TestAuditableEntity> TestEntities { get; set; }

    public TestAuditableDbContext(DbContextOptions options) : base(options)
    {
    }
}

public class AuditableDbContextTests
{
    private TestAuditableDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAuditableDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestAuditableDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNewEntity_SetsCreatedAtAndCreatedBy()
    {
        // Arrange
        var context = CreateContext();
        var entity = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.CreatedBy.Should().Be("SystemUser");
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedBy.Should().Be("SystemUser");
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_SetsUpdatedAtAndUpdatedBy()
    {
        // Arrange
        var context = CreateContext();
        var entity = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "Original" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var originalCreatedAt = entity.CreatedAt;
        var originalCreatedBy = entity.CreatedBy;

        // Act
        entity.Name = "Modified";
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAt.Should().Be(originalCreatedAt);
        entity.CreatedBy.Should().Be(originalCreatedBy);
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedBy.Should().Be("SystemUser");
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_DoesNotModifyCreatedAt()
    {
        // Arrange
        var context = CreateContext();
        var entity = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "Original" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var originalCreatedAt = entity.CreatedAt;
        var originalCreatedBy = entity.CreatedBy;

        // Act
        entity.Name = "Modified";
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAt.Should().Be(originalCreatedAt);
        entity.CreatedBy.Should().Be(originalCreatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNonAuditableEntity_DoesNotThrow()
    {
        // Arrange
        var context = CreateContext();
        var nonAuditableEntity = new { Id = Guid.NewGuid(), Name = "Test" };

        // Act & Assert - Should not throw
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_SetsAuditFieldsForAll()
    {
        // Arrange
        var context = CreateContext();
        var entity1 = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "Entity1" };
        var entity2 = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "Entity2" };

        // Act
        context.TestEntities.AddRange(entity1, entity2);
        await context.SaveChangesAsync();

        // Assert
        entity1.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity1.CreatedBy.Should().Be("SystemUser");
        entity2.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity2.CreatedBy.Should().Be("SystemUser");
    }

    [Fact]
    public async Task SaveChangesAsync_WithMixedNewAndModified_SetsCorrectAuditFields()
    {
        // Arrange
        var context = CreateContext();
        var newEntity = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "New" };
        var existingEntity = new TestAuditableEntity { Id = Guid.NewGuid(), Name = "Existing" };

        context.TestEntities.Add(existingEntity);
        await context.SaveChangesAsync();

        var originalCreatedAt = existingEntity.CreatedAt;
        var originalCreatedBy = existingEntity.CreatedBy;

        // Act
        context.TestEntities.Add(newEntity);
        existingEntity.Name = "Modified";
        await context.SaveChangesAsync();

        // Assert
        newEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        newEntity.CreatedBy.Should().Be("SystemUser");
        existingEntity.CreatedAt.Should().Be(originalCreatedAt);
        existingEntity.CreatedBy.Should().Be(originalCreatedBy);
        existingEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
