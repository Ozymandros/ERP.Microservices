using FluentAssertions;
using MyApp.Audit.Domain;
using MyApp.Audit.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Audit.Application.Tests.Specifications;

public class EntityChangeQuerySpecTests
{
    private static IQueryable<EntityChange> SampleChanges()
    {
        var entityId = Guid.NewGuid();
        return new List<EntityChange>
        {
            new(Guid.NewGuid())
            {
                EntityName = "Product",
                EntityId = entityId,
                ChangeType = ChangeTypeEnum.Updated,
                CreatedAt = new DateTime(2026, 3, 10),
                CreatedBy = "alice@contoso.com"
            },
            new(Guid.NewGuid())
            {
                EntityName = "SalesOrder",
                EntityId = Guid.NewGuid(),
                ChangeType = ChangeTypeEnum.Created,
                CreatedAt = new DateTime(2026, 3, 15),
                CreatedBy = "bob@contoso.com"
            }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_EntityName_Filters()
    {
        var data = SampleChanges();
        var spec = new EntityChangeQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(EntityChange.EntityName), "Product" } }
        });

        var result = spec.ApplyFilters(data).ToList();

        result.Should().ContainSingle(e => e.EntityName == "Product");
    }

    [Fact]
    public void ApplyFilters_ChangeType_Filters()
    {
        var spec = new EntityChangeQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(EntityChange.ChangeType), "Created" } }
        });

        var result = spec.ApplyFilters(SampleChanges()).ToList();

        result.Should().ContainSingle(e => e.ChangeType == ChangeTypeEnum.Created);
    }

    [Fact]
    public void ApplyFilters_CreatedAtRange_Filters()
    {
        var spec = new EntityChangeQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string>
            {
                { "CreatedAtFrom", "2026-03-12" },
                { "CreatedAtTo", "2026-03-20" }
            }
        });

        var result = spec.ApplyFilters(SampleChanges()).ToList();

        result.Should().ContainSingle(e => e.EntityName == "SalesOrder");
    }

    [Fact]
    public void ApplyFilters_SearchTerm_MatchesCreatedBy()
    {
        var spec = new EntityChangeQuerySpec(new QuerySpec { SearchTerm = "alice" });

        var result = spec.ApplyFilters(SampleChanges()).ToList();

        result.Should().ContainSingle(e => e.CreatedBy.Contains("alice", StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_DefaultSort_Paginates()
    {
        var spec = new EntityChangeQuerySpec(new QuerySpec { Page = 1, PageSize = 1 });

        var result = spec.Apply(SampleChanges()).ToList();

        result.Should().ContainSingle();
        result[0].CreatedAt.Should().Be(new DateTime(2026, 3, 15));
    }

    [Fact]
    public void ApplyFilters_EntityId_Filters()
    {
        var data = SampleChanges();
        var entityId = data.First().EntityId;
        var spec = new EntityChangeQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(EntityChange.EntityId), entityId.ToString() } }
        });

        var result = spec.ApplyFilters(data).ToList();

        result.Should().ContainSingle(e => e.EntityId == entityId);
    }

    [Fact]
    public void ApplyFilters_CreatedBy_Filters()
    {
        var spec = new EntityChangeQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(EntityChange.CreatedBy), "bob" } }
        });

        var result = spec.ApplyFilters(SampleChanges()).ToList();

        result.Should().ContainSingle(e => e.CreatedBy.StartsWith("bob"));
    }
}