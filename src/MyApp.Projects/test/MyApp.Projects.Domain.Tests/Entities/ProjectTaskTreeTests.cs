using FluentAssertions;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Services;
using Xunit;

namespace MyApp.Projects.Domain.Tests.Entities;

/// <summary>Tests for <see cref="ProjectTaskTree"/>.</summary>
public class ProjectTaskTreeTests
{
    // root
    //  ├─ a
    //  │   └─ a1
    //  └─ b
    private readonly ProjectTask _root = new(Guid.NewGuid()) { TaskNo = "1000", TaskType = ProjectTaskType.Heading };
    private readonly ProjectTask _a;
    private readonly ProjectTask _a1;
    private readonly ProjectTask _b;
    private readonly List<ProjectTask> _tasks;

    public ProjectTaskTreeTests()
    {
        _a = new ProjectTask(Guid.NewGuid()) { TaskNo = "1100", ParentTaskId = _root.Id, TaskType = ProjectTaskType.Heading };
        _a1 = new ProjectTask(Guid.NewGuid()) { TaskNo = "1110", ParentTaskId = _a.Id };
        _b = new ProjectTask(Guid.NewGuid()) { TaskNo = "1200", ParentTaskId = _root.Id };
        _tasks = new List<ProjectTask> { _root, _a, _a1, _b };
    }

    [Fact]
    public void WouldCreateCycle_MoveUnderOwnDescendant_ReturnsTrue()
    {
        ProjectTaskTree.WouldCreateCycle(_tasks, _root.Id, _a1.Id).Should().BeTrue();
        ProjectTaskTree.WouldCreateCycle(_tasks, _a.Id, _a.Id).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_MoveUnderSiblingOrRoot_ReturnsFalse()
    {
        ProjectTaskTree.WouldCreateCycle(_tasks, _b.Id, _a.Id).Should().BeFalse();
        ProjectTaskTree.WouldCreateCycle(_tasks, _a1.Id, null).Should().BeFalse();
    }

    [Fact]
    public void GetDescendantIds_ReturnsWholeSubtree()
    {
        ProjectTaskTree.GetDescendantIds(_tasks, _root.Id).Should().BeEquivalentTo(new[] { _a.Id, _a1.Id, _b.Id });
        ProjectTaskTree.GetDescendantIds(_tasks, _b.Id).Should().BeEmpty();
    }

    [Fact]
    public void HasChildren_Works()
    {
        ProjectTaskTree.HasChildren(_tasks, _a.Id).Should().BeTrue();
        ProjectTaskTree.HasChildren(_tasks, _a1.Id).Should().BeFalse();
    }

    [Fact]
    public void RollUp_SumsDescendantsIntoAncestors()
    {
        var own = new Dictionary<Guid, decimal> { [_a1.Id] = 10m, [_b.Id] = 5m, [_a.Id] = 1m };

        var result = ProjectTaskTree.RollUp<decimal>(_tasks, own, (x, y) => x + y, () => 0m);

        result[_a1.Id].Should().Be(10m);
        result[_a.Id].Should().Be(11m);
        result[_b.Id].Should().Be(5m);
        result[_root.Id].Should().Be(16m);
    }
}
