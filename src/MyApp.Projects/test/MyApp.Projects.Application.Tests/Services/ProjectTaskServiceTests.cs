using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

/// <summary>Tests for <see cref="ProjectTaskService"/> tree rules.</summary>
public class ProjectTaskServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IProjectTaskRepository> _tasks = new();
    private readonly Mock<IProjectPlanningLineRepository> _lines = new();
    private readonly Mock<IProjectLedgerEntryRepository> _ledger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProjectTaskService _sut;
    private readonly Project _project;
    private readonly List<ProjectTask> _projectTasks = new();

    public ProjectTaskServiceTests()
    {
        _project = new Project(Guid.NewGuid()) { ProjectNumber = "PRJ-2026-00002", CurrencyCode = "EUR" };
        _projects.Setup(r => r.GetByIdAsync(_project.Id)).ReturnsAsync(_project);
        _tasks.Setup(r => r.GetByProjectAsync(_project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_projectTasks);
        _tasks.Setup(r => r.AddAsync(It.IsAny<ProjectTask>()))
              .Callback<ProjectTask>(_projectTasks.Add)
              .Returns<ProjectTask>(t => Task.FromResult(t));
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<EntityEntryDto>());

        _sut = new ProjectTaskService(
            _projects.Object, _tasks.Object, _lines.Object, _ledger.Object, Mock.Of<IMapper>(),
            _unitOfWork.Object, Mock.Of<IEventPublisher>(), Mock.Of<ILogger<ProjectTaskService>>());
    }

    private ProjectTask AddTask(string taskNo, Guid? parentId = null, ProjectTaskType type = ProjectTaskType.Posting)
    {
        var task = new ProjectTask(Guid.NewGuid()) { ProjectId = _project.Id, TaskNo = taskNo, ParentTaskId = parentId, TaskType = type };
        _projectTasks.Add(task);
        return task;
    }

    [Fact]
    public async Task CreateAsync_UnderPostingTaskWithoutData_PromotesParentToHeading()
    {
        var parent = AddTask("1000");

        await _sut.CreateAsync(_project.Id, new CreateProjectTaskDto { ParentTaskId = parent.Id, TaskNo = "1010", Description = "Design" });

        parent.TaskType.Should().Be(ProjectTaskType.Heading);
        _projectTasks.Should().Contain(t => t.TaskNo == "1010" && t.ParentTaskId == parent.Id);
    }

    [Fact]
    public async Task CreateAsync_UnderPostingTaskWithPlanningLines_Throws()
    {
        var parent = AddTask("1000");
        _lines.Setup(r => r.AnyForTaskAsync(parent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(_project.Id, new CreateProjectTaskDto { ParentTaskId = parent.Id, TaskNo = "1010", Description = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>();
        parent.TaskType.Should().Be(ProjectTaskType.Posting);
    }

    [Fact]
    public async Task CreateAsync_DuplicateTaskNo_Throws()
    {
        AddTask("1000");

        var act = () => _sut.CreateAsync(_project.Id, new CreateProjectTaskDto { TaskNo = "1000", Description = "dup" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MoveAsync_UnderOwnDescendant_Throws()
    {
        var root = AddTask("1000", type: ProjectTaskType.Heading);
        var child = AddTask("1100", root.Id);

        var act = () => _sut.MoveAsync(_project.Id, root.Id, new MoveProjectTaskDto { NewParentTaskId = child.Id });

        await act.Should().ThrowAsync<InvalidOperationException>();
        root.ParentTaskId.Should().BeNull();
    }

    [Fact]
    public async Task MoveAsync_ToAnotherHeading_ReparentsAndAppends()
    {
        var a = AddTask("1000", type: ProjectTaskType.Heading);
        var b = AddTask("2000", type: ProjectTaskType.Heading);
        var existing = AddTask("2100", b.Id);
        existing.SortOrder = 10;
        var moving = AddTask("1100", a.Id);

        await _sut.MoveAsync(_project.Id, moving.Id, new MoveProjectTaskDto { NewParentTaskId = b.Id });

        moving.ParentTaskId.Should().Be(b.Id);
        moving.SortOrder.Should().Be(20);
    }

    [Fact]
    public async Task DeleteAsync_TaskWithChildren_Throws()
    {
        var root = AddTask("1000", type: ProjectTaskType.Heading);
        AddTask("1100", root.Id);

        var act = () => _sut.DeleteAsync(_project.Id, root.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AnyOperation_OnCompletedProject_Throws()
    {
        _project.Status = ProjectStatus.Completed;

        var act = () => _sut.CreateAsync(_project.Id, new CreateProjectTaskDto { TaskNo = "9000", Description = "late" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
