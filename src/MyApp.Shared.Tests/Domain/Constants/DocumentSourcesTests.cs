using FluentAssertions;
using MyApp.Shared.Domain.Constants;

namespace MyApp.Shared.Tests.Domain.Constants;

public class DocumentSourcesTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("Sales", true)]
    [InlineData("sales", true)]
    [InlineData("Project", false)]
    [InlineData("project", false)]
    public void IsSales_RecognizesSalesAndLegacyNullAsSales(string? source, bool expected) =>
        DocumentSources.IsSales(source).Should().Be(expected);

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Sales", false)]
    [InlineData("Project", true)]
    [InlineData("project", true)]
    public void IsProject_RecognizesProjectOnly(string? source, bool expected) =>
        DocumentSources.IsProject(source).Should().Be(expected);
}