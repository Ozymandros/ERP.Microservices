using FluentAssertions;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Projects.Domain.Tests.Specifications;

public class ResourceQuerySpecTests
{
    private static IQueryable<Resource> SampleResources()
    {
        var groupId = Guid.NewGuid();
        return new List<Resource>
        {
            new(Guid.NewGuid())
            {
                ResourceNo = "R-100",
                Name = "Consultant",
                Type = ResourceType.Person,
                ResourceGroupId = groupId,
                Blocked = false
            },
            new(Guid.NewGuid())
            {
                ResourceNo = "M-200",
                Name = "3D Printer",
                Type = ResourceType.Machine,
                Blocked = true
            }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_Type_FiltersPersons()
    {
        var spec = new ResourceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Resource.Type), "Person" } }
        });

        var result = spec.ApplyFilters(SampleResources()).ToList();

        result.Should().ContainSingle(r => r.ResourceNo == "R-100");
    }

    [Fact]
    public void ApplyFilters_ResourceGroupId_Filters()
    {
        var resources = SampleResources();
        var groupId = resources.First().ResourceGroupId!.Value;
        var spec = new ResourceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Resource.ResourceGroupId), groupId.ToString() } }
        });

        var result = spec.ApplyFilters(resources).ToList();

        result.Should().ContainSingle(r => r.ResourceGroupId == groupId);
    }

    [Fact]
    public void ApplyFilters_Blocked_Filters()
    {
        var spec = new ResourceQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Resource.Blocked), "true" } }
        });

        var result = spec.ApplyFilters(SampleResources()).ToList();

        result.Should().ContainSingle(r => r.Blocked);
    }

    [Fact]
    public void ApplyFilters_SearchTerm_MatchesResourceNo()
    {
        var spec = new ResourceQuerySpec(new QuerySpec { SearchTerm = "M-200" });

        var result = spec.ApplyFilters(SampleResources()).ToList();

        result.Should().ContainSingle(r => r.Name == "3D Printer");
    }
}