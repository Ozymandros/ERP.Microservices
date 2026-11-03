using FluentAssertions;
using MyApp.Projects.Domain.Entities;
using Xunit;

namespace MyApp.Projects.Domain.Tests.Entities;

/// <summary>Tests for <see cref="Project"/> lifecycle rules.</summary>
public class ProjectTests
{
    private static Project NewProject(ProjectStatus status = ProjectStatus.Planning) =>
        new(Guid.NewGuid()) { ProjectNumber = "PRJ-2026-00001", Status = status, CurrencyCode = "EUR" };

    [Theory]
    [InlineData(ProjectStatus.Planning, ProjectStatus.Quote)]
    [InlineData(ProjectStatus.Planning, ProjectStatus.Open)]
    [InlineData(ProjectStatus.Quote, ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Quote, ProjectStatus.Open)]
    [InlineData(ProjectStatus.Open, ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Open)]
    public void ChangeStatus_AllowedTransition_Succeeds(ProjectStatus from, ProjectStatus to)
    {
        var project = NewProject(from);

        project.ChangeStatus(to);

        project.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(ProjectStatus.Planning, ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Open, ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Open, ProjectStatus.Quote)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Planning)]
    public void ChangeStatus_ForbiddenTransition_Throws(ProjectStatus from, ProjectStatus to)
    {
        var project = NewProject(from);

        var act = () => project.ChangeStatus(to);

        act.Should().Throw<InvalidOperationException>();
        project.Status.Should().Be(from);
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Quote)]
    [InlineData(ProjectStatus.Completed)]
    public void EnsureCanPost_NotOpen_Throws(ProjectStatus status)
    {
        var act = () => NewProject(status).EnsureCanPost();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ProjectBlocked.Posting)]
    [InlineData(ProjectBlocked.All)]
    public void EnsureCanPost_Blocked_Throws(ProjectBlocked blocked)
    {
        var project = NewProject(ProjectStatus.Open);
        project.Blocked = blocked;

        var act = () => project.EnsureCanPost();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureCanPost_OpenAndNotBlocked_DoesNotThrow()
    {
        var act = () => NewProject(ProjectStatus.Open).EnsureCanPost();

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanModify_PostingBlocked_AllowsPlanning()
    {
        var project = NewProject(ProjectStatus.Open);
        project.Blocked = ProjectBlocked.Posting;

        var act = () => project.EnsureCanModify();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateDates_EndBeforeStart_Throws()
    {
        var project = NewProject();
        project.StartingDate = new DateTime(2026, 5, 10);
        project.EndingDate = new DateTime(2026, 5, 1);

        var act = () => project.ValidateDates();

        act.Should().Throw<ArgumentException>();
    }
}
