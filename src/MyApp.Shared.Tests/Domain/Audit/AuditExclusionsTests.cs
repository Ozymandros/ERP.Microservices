using FluentAssertions;
using MyApp.Shared.Domain.Audit;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Shared.Tests.Domain.Audit;

public class AuditExclusionsTests
{
    [Theory]
    [InlineData("RefreshToken", true)]
    [InlineData("AgentMemory", true)]
    [InlineData("AgentSession", true)]
    [InlineData("EntityChange", true)]
    [InlineData("PropertyChange", true)]
    [InlineData("Product", false)]
    [InlineData("ApplicationRole", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsExcluded_MatchesPolicy(string? entityName, bool expected)
    {
        AuditExclusions.IsExcluded(entityName).Should().Be(expected);
    }

    [Fact]
    public void FilterForAudit_RemovesAllExcludedEntries()
    {
        var changes = new List<EntityEntryDto>
        {
            new("RefreshToken", Guid.NewGuid(), "Added", []),
            new("AgentSession", Guid.NewGuid(), "Modified", []),
            new("EntityChange", Guid.NewGuid(), "Added", []),
            new("PropertyChange", Guid.NewGuid(), "Added", []),
            new("Invoice", Guid.NewGuid(), "Modified", [])
        };

        var filtered = AuditExclusions.FilterForAudit(changes, c => c.EntityName);

        filtered.Should().ContainSingle().Which.EntityName.Should().Be("Invoice");
    }
}
