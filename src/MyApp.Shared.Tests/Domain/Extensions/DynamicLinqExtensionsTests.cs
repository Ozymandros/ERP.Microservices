using FluentAssertions;
using MyApp.Shared.Domain.Extensions;

namespace MyApp.Shared.Tests.Domain.Extensions;

public class DynamicLinqExtensionsTests
{
    private sealed class SampleEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }

  private static readonly SampleEntity[] Data =
  [
      new() { Id = 2, Name = "Beta", Code = "B2" },
      new() { Id = 1, Name = "Alpha", Code = "A1" }
  ];

    [Fact]
    public void OrderByDynamic_SortsAscendingByPropertyName()
    {
        var ordered = Data.AsQueryable().OrderByDynamic(nameof(SampleEntity.Id)).ToList();
        ordered.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void OrderByDynamic_SortsDescendingWhenRequested()
    {
        var ordered = Data.AsQueryable().OrderByDynamic(nameof(SampleEntity.Id), descending: true).ToList();
        ordered.Select(x => x.Id).Should().Equal(2, 1);
    }

    [Fact]
    public void OrderByDynamic_InvalidProperty_ReturnsUnchangedQuery()
    {
        var query = Data.AsQueryable();
        query.OrderByDynamic("Missing").ToList().Should().HaveCount(2);
    }

    [Fact]
    public void FilterByProperty_FiltersOnEquality()
    {
        var filtered = Data.AsQueryable().FilterByProperty(nameof(SampleEntity.Name), "Alpha").ToList();
        filtered.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void FilterByMultipleProperties_MatchesAnyStringProperty()
    {
        var filtered = Data.AsQueryable()
            .FilterByMultipleProperties("lph", nameof(SampleEntity.Name), nameof(SampleEntity.Code))
            .ToList();
        filtered.Should().ContainSingle().Which.Name.Should().Be("Alpha");
    }

    [Fact]
    public void FilterByMultipleProperties_EmptySearch_ReturnsAll()
    {
        Data.AsQueryable()
            .FilterByMultipleProperties("", nameof(SampleEntity.Name))
            .Should().HaveCount(2);
    }
}