using FluentAssertions;
using Xunit;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Projects.Domain.Tests.Specifications;

public class ProjectQuerySpecTests
{
    private static IQueryable<Project> SampleProjects()
    {
        var customer = Guid.NewGuid();
        return new List<Project>
        {
            new(Guid.NewGuid())
            {
                ProjectNumber = "PRJ-001",
                CustomerId = customer,
                CurrencyCode = "USD",
                Status = ProjectStatus.Open,
                Description = "Alpha build"
            },
            new(Guid.NewGuid())
            {
                ProjectNumber = "PRJ-002",
                CustomerId = Guid.NewGuid(),
                CurrencyCode = "EUR",
                Status = ProjectStatus.Planning,
                ExternalReference = "PO-999"
            }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_Status_FiltersOpenProjects()
    {
        var spec = new ProjectQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Project.Status), "Open" } }
        });

        var result = spec.ApplyFilters(SampleProjects()).ToList();

        result.Should().ContainSingle(p => p.ProjectNumber == "PRJ-001");
    }

    [Fact]
    public void ApplyFilters_CustomerId_Filters()
    {
        var projects = SampleProjects();
        var customerId = projects.First().CustomerId;
        var spec = new ProjectQuerySpec(new QuerySpec
        {
            Filters = new Dictionary<string, string> { { nameof(Project.CustomerId), customerId.ToString() } }
        });

        var result = spec.ApplyFilters(projects).ToList();

        result.Should().ContainSingle(p => p.CustomerId == customerId);
    }

    [Fact]
    public void ApplyFilters_SearchTerm_MatchesExternalReference()
    {
        var spec = new ProjectQuerySpec(new QuerySpec { SearchTerm = "PO-999" });

        var result = spec.ApplyFilters(SampleProjects()).ToList();

        result.Should().ContainSingle(p => p.ExternalReference == "PO-999");
    }
}