using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Integration;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

/// <summary>Tests for <see cref="PlanningLineService"/> defaults and multi-currency pricing.</summary>
public class PlanningLineServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IProjectTaskRepository> _tasks = new();
    private readonly Mock<IProjectPlanningLineRepository> _lines = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<ICurrencyService> _currency = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PlanningLineService _sut;
    private readonly Project _project;
    private readonly ProjectTask _task;
    private readonly Resource _resource;
    private ProjectPlanningLine? _added;

    public PlanningLineServiceTests()
    {
        _project = new Project(Guid.NewGuid()) { ProjectNumber = "PRJ-2026-00003", CurrencyCode = "USD" };
        _task = new ProjectTask(Guid.NewGuid()) { ProjectId = _project.Id, TaskNo = "1000" };
        _resource = new Resource(Guid.NewGuid()) { ResourceNo = "R0001", Name = "Senior Consultant", UnitCost = 45m, UnitPrice = 90m, BaseUnitOfMeasure = "HOUR" };

        _projects.Setup(r => r.GetByIdAsync(_project.Id)).ReturnsAsync(_project);
        _tasks.Setup(r => r.GetByProjectAsync(_project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ProjectTask> { _task });
        _resources.Setup(r => r.GetByIdAsync(_resource.Id)).ReturnsAsync(_resource);
        _lines.Setup(r => r.AddAsync(It.IsAny<ProjectPlanningLine>()))
              .Callback<ProjectPlanningLine>(l => _added = l)
              .Returns<ProjectPlanningLine>(l => Task.FromResult(l));
        _currency.Setup(c => c.GetRateAsync("USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0.9m);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<EntityEntryDto>());

        _sut = new PlanningLineService(
            _projects.Object, _tasks.Object, _lines.Object, Mock.Of<IProjectLedgerEntryRepository>(), _resources.Object,
            Mock.Of<IInventoryProductClient>(), _currency.Object, Mock.Of<IMapper>(), _unitOfWork.Object,
            Mock.Of<IEventPublisher>(), Mock.Of<ILogger<PlanningLineService>>());
    }

    [Fact]
    public async Task CreateAsync_ResourceLine_DefaultsFromResource_AndConvertsPriceToProjectCurrency()
    {
        await _sut.CreateAsync(_project.Id, new CreateUpdatePlanningLineDto
        {
            ProjectTaskId = _task.Id,
            Kind = ProjectLineKind.Resource,
            ResourceId = _resource.Id,
            Quantity = 8m
        });

        _added.Should().NotBeNull();
        _added!.Description.Should().Be("Senior Consultant");
        _added.UnitOfMeasure.Should().Be("HOUR");
        _added.CurrencyCode.Should().Be("USD");
        _added.ExchangeRate.Should().Be(0.9m);
        _added.UnitCostLcy.Should().Be(45m);
        _added.UnitPrice.Should().Be(100m);          // 90 EUR / 0.9 = 100 USD
        _added.TotalCostLcy.Should().Be(360m);
        _added.LineAmount.Should().Be(800m);
        _added.LineAmountLcy.Should().Be(720m);
        _added.LineNo.Should().Be(10000);
    }

    [Fact]
    public async Task CreateAsync_OnHeadingTask_Throws()
    {
        _task.TaskType = ProjectTaskType.Heading;

        var act = () => _sut.CreateAsync(_project.Id, new CreateUpdatePlanningLineDto
        {
            ProjectTaskId = _task.Id, Kind = ProjectLineKind.Resource, ResourceId = _resource.Id, Quantity = 1
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_BlockedResource_Throws()
    {
        _resource.Blocked = true;

        var act = () => _sut.CreateAsync(_project.Id, new CreateUpdatePlanningLineDto
        {
            ProjectTaskId = _task.Id, Kind = ProjectLineKind.Resource, ResourceId = _resource.Id, Quantity = 1
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
