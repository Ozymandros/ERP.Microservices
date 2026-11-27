using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Contracts.Ports;
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
        _resources.Setup(r => r.GetByIdAsync(_resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_resource);
        _projects.Setup(r => r.GetByIdAsync(_project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_project);
        _lines.Setup(r => r.AddAsync(It.IsAny<ProjectPlanningLine>()))
              .Callback<ProjectPlanningLine>(l => _added = l)
              .Returns<ProjectPlanningLine>(l => Task.FromResult(l));
        _currency.Setup(c => c.GetRateAsync("USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0.9m);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<EntityEntryDto>());

        _sut = new PlanningLineService(
            _projects.Object, _tasks.Object, _lines.Object, Mock.Of<IProjectLedgerEntryRepository>(), _resources.Object,
            Mock.Of<IInventoryProductLookupPort>(), _currency.Object, Mock.Of<IMapper>(), _unitOfWork.Object,
            Mock.Of<IEventPublisher>(), Mock.Of<ILogger<PlanningLineService>>());
    }

    [Fact]
    public async Task GetByIdAsync_MatchingProject_ReturnsMappedDto()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            ProjectTaskId = _task.Id,
            Description = "Line"
        };
        var dto = new PlanningLineDto(line.Id) { Description = "Line" };
        _lines.Setup(r => r.GetByIdAsync(line.Id, It.IsAny<CancellationToken>())).ReturnsAsync(line);
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<PlanningLineDto>(line)).Returns(dto);

        var sut = new PlanningLineService(
            _projects.Object, _tasks.Object, _lines.Object, Mock.Of<IProjectLedgerEntryRepository>(), _resources.Object,
            Mock.Of<IInventoryProductLookupPort>(), _currency.Object, mapper.Object, _unitOfWork.Object,
            Mock.Of<IEventPublisher>(), Mock.Of<ILogger<PlanningLineService>>());

        var result = await sut.GetByIdAsync(_project.Id, line.Id);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Line");
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

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesQuantityAndRecalculates()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            ProjectTaskId = _task.Id,
            Kind = ProjectLineKind.Resource,
            ResourceId = _resource.Id,
            Description = "Senior Consultant",
            UnitOfMeasure = "HOUR",
            Quantity = 4m,
            UnitCostLcy = 45m,
            UnitPrice = 100m,
            CurrencyCode = "USD",
            ExchangeRate = 0.9m
        };
        line.Recalculate();
        _lines.Setup(r => r.GetByIdAsync(line.Id, It.IsAny<CancellationToken>())).ReturnsAsync(line);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<PlanningLineDto>(line))
            .Returns((ProjectPlanningLine l) => new PlanningLineDto(l.Id)
            {
                Description = l.Description,
                Quantity = l.Quantity,
                LineAmount = l.LineAmount
            });
        var sut = CreateSut(mapper.Object);

        var result = await sut.UpdateAsync(_project.Id, line.Id, new CreateUpdatePlanningLineDto
        {
            ProjectTaskId = _task.Id,
            Kind = ProjectLineKind.Resource,
            ResourceId = _resource.Id,
            Quantity = 10m,
            UnitPrice = 100m,
            UnitCostLcy = 45m
        });

        line.Quantity.Should().Be(10m);
        result.Quantity.Should().Be(10m);
        result.LineAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task UpdateAsync_ChangeKindAfterActivity_Throws()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            ProjectTaskId = _task.Id,
            Kind = ProjectLineKind.Resource,
            ResourceId = _resource.Id,
            Quantity = 4m,
            QuantityPosted = 1m
        };
        _lines.Setup(r => r.GetByIdAsync(line.Id, It.IsAny<CancellationToken>())).ReturnsAsync(line);

        var act = () => _sut.UpdateAsync(_project.Id, line.Id, new CreateUpdatePlanningLineDto
        {
            ProjectTaskId = _task.Id,
            Kind = ProjectLineKind.Expense,
            Description = "Expense",
            Quantity = 4m
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot be changed*");
    }

    [Fact]
    public async Task DeleteAsync_WithoutActivity_Deletes()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            ProjectTaskId = _task.Id,
            Description = "Line"
        };
        var ledger = new Mock<IProjectLedgerEntryRepository>();
        ledger.Setup(r => r.AnyForPlanningLineAsync(line.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _lines.Setup(r => r.GetByIdAsync(line.Id, It.IsAny<CancellationToken>())).ReturnsAsync(line);
        _lines.Setup(r => r.DeleteAsync(line)).Returns(Task.CompletedTask);

        var sut = new PlanningLineService(
            _projects.Object, _tasks.Object, _lines.Object, ledger.Object, _resources.Object,
            Mock.Of<IInventoryProductLookupPort>(), _currency.Object, Mock.Of<IMapper>(), _unitOfWork.Object,
            Mock.Of<IEventPublisher>(), Mock.Of<ILogger<PlanningLineService>>());

        await sut.DeleteAsync(_project.Id, line.Id);

        _lines.Verify(r => r.DeleteAsync(line), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithPostedQuantity_Throws()
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            ProjectTaskId = _task.Id,
            QuantityPosted = 1m
        };
        _lines.Setup(r => r.GetByIdAsync(line.Id, It.IsAny<CancellationToken>())).ReturnsAsync(line);

        var act = () => _sut.DeleteAsync(_project.Id, line.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot be deleted*");
    }

    private PlanningLineService CreateSut(IMapper mapper) =>
        new(_projects.Object, _tasks.Object, _lines.Object, Mock.Of<IProjectLedgerEntryRepository>(), _resources.Object,
            Mock.Of<IInventoryProductLookupPort>(), _currency.Object, mapper, _unitOfWork.Object,
            Mock.Of<IEventPublisher>(), Mock.Of<ILogger<PlanningLineService>>());
}
