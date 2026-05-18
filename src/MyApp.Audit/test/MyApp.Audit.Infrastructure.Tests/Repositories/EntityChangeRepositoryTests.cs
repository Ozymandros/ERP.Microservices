using FluentAssertions;
using MyApp.Audit.Domain;
using MyApp.Audit.Domain.Specifications;
using MyApp.Audit.Infrastructure.Repositories;
using MyApp.Audit.Infrastructure.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Audit.Infrastructure.Tests.Repositories;

public class EntityChangeRepositoryTests
{
    [Fact]
    public async Task GetByIdWithPropertiesAsync_IncludesPropertyChanges()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EntityChangeRepository(context);

        var changeId = Guid.NewGuid();
        var entityChange = new EntityChange(changeId)
        {
            EntityName = "Product",
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeTypeEnum.Updated,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tester"
        };
        entityChange.PropertyChanges.Add(new PropertyChange(Guid.NewGuid())
        {
            EntityChangeId = changeId,
            PropertyName = "Name",
            OriginalValue = "A",
            NewValue = "B"
        });

        context.EntityChanges.Add(entityChange);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdWithPropertiesAsync(changeId);

        result.Should().NotBeNull();
        result!.PropertyChanges.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByEntityAsync_FiltersAndOrdersByCreatedAtDescending()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EntityChangeRepository(context);

        var entityId = Guid.NewGuid();
        var older = new EntityChange(Guid.NewGuid())
        {
            EntityName = "Order",
            EntityId = entityId,
            ChangeType = ChangeTypeEnum.Created,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            CreatedBy = "a"
        };
        var newer = new EntityChange(Guid.NewGuid())
        {
            EntityName = "Order",
            EntityId = entityId,
            ChangeType = ChangeTypeEnum.Updated,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "b"
        };
        var other = new EntityChange(Guid.NewGuid())
        {
            EntityName = "Order",
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeTypeEnum.Created,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "c"
        };

        context.EntityChanges.AddRange(older, newer, other);
        await context.SaveChangesAsync();

        var result = await repository.GetByEntityAsync("Order", entityId);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(newer.Id);
        result[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task QueryAsync_FiltersByEntityName()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EntityChangeRepository(context);

        context.EntityChanges.AddRange(
            new EntityChange(Guid.NewGuid())
            {
                EntityName = "Product",
                EntityId = Guid.NewGuid(),
                ChangeType = ChangeTypeEnum.Created,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "x"
            },
            new EntityChange(Guid.NewGuid())
            {
                EntityName = "Customer",
                EntityId = Guid.NewGuid(),
                ChangeType = ChangeTypeEnum.Created,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "y"
            });
        await context.SaveChangesAsync();

        var query = new QuerySpec
        {
            Page = 1,
            PageSize = 10,
            Filters = new Dictionary<string, string> { [nameof(EntityChange.EntityName)] = "Product" }
        };

        var result = await repository.QueryAsync(new EntityChangeQuerySpec(query));

        result.Items.Should().HaveCount(1);
        result.Items.First().EntityName.Should().Be("Product");
    }
}
