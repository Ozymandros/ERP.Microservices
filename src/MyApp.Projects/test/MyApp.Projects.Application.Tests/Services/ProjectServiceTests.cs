using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.Application.Contracts;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Ports;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IProjectTaskRepository> _tasks = new();
    private readonly Mock<IProjectPlanningLineRepository> _planningLines = new();
    private readonly Mock<IProjectLedgerEntryRepository> _ledger = new();
    private readonly Mock<ICurrencyService> _currency = new();
    private readonly Mock<ISalesCustomerLookupPort> _customers = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _publisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _currency.SetupGet(c => c.LocalCurrency).Returns("EUR");
        _mapper.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectDto(p.Id)
            {
                ProjectNumber = p.ProjectNumber,
                Description = p.Description,
                CustomerId = p.CustomerId,
                CurrencyCode = p.CurrencyCode,
                Status = p.Status.ToString()
            });

        _sut = new ProjectService(
            _projects.Object,
            _tasks.Object,
            _planningLines.Object,
            _ledger.Object,
            _currency.Object,
            _customers.Object,
            new ProjectsSettings(),
            _mapper.Object,
            _uow.Object,
            _publisher.Object,
            Mock.Of<ILogger<ProjectService>>());
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var project = new Project(id) { ProjectNumber = "PRJ-1", Description = "Demo" };
        _projects.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.ProjectNumber.Should().Be("PRJ-1");
    }

    [Fact]
    public async Task QueryAsync_ReturnsPaginatedMappedDtos()
    {
        var project = new Project(Guid.NewGuid()) { ProjectNumber = "PRJ-2" };
        var spec = Mock.Of<ISpecification<Project>>();
        var page = new PaginatedResult<Project>([project], 1, 10, 1);
        _projects.Setup(r => r.QueryAsync(spec, It.IsAny<CancellationToken>())).ReturnsAsync(page);
        _mapper.Setup(m => m.Map<List<ProjectDto>>(page.Items))
            .Returns([new ProjectDto(project.Id) { ProjectNumber = "PRJ-2" }]);

        var result = await _sut.QueryAsync(spec);

        result.Items.Should().ContainSingle(p => p.ProjectNumber == "PRJ-2");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesProjectAndPublishes()
    {
        var customerId = Guid.NewGuid();
        _customers.Setup(c => c.GetCustomerAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesCustomerSnapshot(customerId, "Acme"));
        _projects.Setup(r => r.CountByNumberPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _projects.Setup(r => r.GetByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);
        _projects.Setup(r => r.AddAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);

        var result = await _sut.CreateAsync(new CreateProjectDto
        {
            Description = " New project ",
            CustomerId = customerId
        });

        result.Description.Should().Be("New project");
        result.CustomerId.Should().Be(customerId);
        result.CurrencyCode.Should().Be("EUR");
        result.Status.Should().Be(ProjectStatus.Planning.ToString());
        _publisher.Verify(p => p.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<ProjectCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_AllowedTransition_UpdatesAndPublishes()
    {
        var project = new Project(Guid.NewGuid())
        {
            ProjectNumber = "PRJ-1",
            Status = ProjectStatus.Planning
        };
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var result = await _sut.ChangeStatusAsync(project.Id, ProjectStatus.Open);

        result.Status.Should().Be(ProjectStatus.Open.ToString());
        project.Status.Should().Be(ProjectStatus.Open);
        _publisher.Verify(p => p.PublishAsync(
            It.IsAny<string>(),
            It.Is<ProjectStatusChangedEvent>(e => e.OldStatus == "Planning" && e.NewStatus == "Open"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_SameStatus_DoesNotPublish()
    {
        var project = new Project(Guid.NewGuid()) { ProjectNumber = "PRJ-1", Status = ProjectStatus.Open };
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _sut.ChangeStatusAsync(project.Id, ProjectStatus.Open);

        _publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyAsync_CopiesTasksAndPlanningLines()
    {
        var source = new Project(Guid.NewGuid())
        {
            ProjectNumber = "PRJ-SRC",
            Description = "Source",
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "EUR",
            Status = ProjectStatus.Open
        };
        var task = new ProjectTask(Guid.NewGuid())
        {
            ProjectId = source.Id,
            TaskNo = "1000",
            Description = "Task",
            TaskType = ProjectTaskType.Posting
        };
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = source.Id,
            ProjectTaskId = task.Id,
            LineNo = 10000,
            Description = "Line",
            Quantity = 2m,
            UnitCostLcy = 10m,
            UnitPrice = 20m,
            CurrencyCode = "EUR",
            ExchangeRate = 1m
        };
        line.Recalculate();

        _projects.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _projects.Setup(r => r.CountByNumberPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _projects.Setup(r => r.GetByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);
        _projects.Setup(r => r.AddAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);
        _tasks.Setup(r => r.GetByProjectAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        _tasks.Setup(r => r.AddAsync(It.IsAny<ProjectTask>())).ReturnsAsync((ProjectTask t) => t);
        _planningLines.Setup(r => r.GetByProjectAsync(source.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync([line]);
        _planningLines.Setup(r => r.AddAsync(It.IsAny<ProjectPlanningLine>())).ReturnsAsync((ProjectPlanningLine l) => l);

        var result = await _sut.CopyAsync(source.Id, new CopyProjectDto { IncludePlanningLines = true });

        result.Description.Should().StartWith("Copy of");
        result.Status.Should().Be(ProjectStatus.Planning.ToString());
        _tasks.Verify(r => r.AddAsync(It.Is<ProjectTask>(t => t.TaskNo == "1000")), Times.Once);
        _planningLines.Verify(r => r.AddAsync(It.Is<ProjectPlanningLine>(l => l.Description == "Line")), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PlanningWithoutLedger_Deletes()
    {
        var project = new Project(Guid.NewGuid())
        {
            ProjectNumber = "PRJ-1",
            Status = ProjectStatus.Planning
        };
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _ledger.Setup(r => r.AnyForProjectAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _projects.Setup(r => r.DeleteAsync(project)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(project.Id);

        _projects.Verify(r => r.DeleteAsync(project), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_OpenStatus_Throws()
    {
        var project = new Project(Guid.NewGuid()) { ProjectNumber = "PRJ-1", Status = ProjectStatus.Open };
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = () => _sut.DeleteAsync(project.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Planning or Quote*");
    }
}

public class ProjectStatisticsCalculatorTests
{
    [Fact]
    public void CalculateOwnAmounts_BudgetAndBillableLine_FillsBudgetAndBillable()
    {
        var taskId = Guid.NewGuid();
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectTaskId = taskId,
            LineType = PlanningLineType.BudgetAndBillable,
            Quantity = 2m,
            UnitCostLcy = 10m,
            UnitPrice = 20m,
            ExchangeRate = 1m
        };
        line.Recalculate();

        var result = ProjectStatisticsCalculator.CalculateOwnAmounts([line], []);

        result[taskId].BudgetCostLcy.Should().Be(20m);
        result[taskId].BudgetPrice.Should().Be(40m);
        result[taskId].BillablePrice.Should().Be(40m);
    }

    [Fact]
    public void CalculateOwnAmounts_UsageAndSaleEntries_Accumulate()
    {
        var taskId = Guid.NewGuid();
        var usage = new ProjectLedgerEntry(Guid.NewGuid())
        {
            ProjectTaskId = taskId,
            EntryType = ProjectLedgerEntryType.Usage,
            TotalCostLcy = 15m,
            LineAmount = 30m
        };
        var sale = new ProjectLedgerEntry(Guid.NewGuid())
        {
            ProjectTaskId = taskId,
            EntryType = ProjectLedgerEntryType.Sale,
            LineAmount = 50m,
            LineAmountLcy = 45m
        };

        var result = ProjectStatisticsCalculator.CalculateOwnAmounts([], [usage, sale]);

        result[taskId].UsageCostLcy.Should().Be(15m);
        result[taskId].UsagePrice.Should().Be(30m);
        result[taskId].InvoicedPrice.Should().Be(50m);
        result[taskId].InvoicedPriceLcy.Should().Be(45m);
    }

    [Fact]
    public void CalculateOwnAmounts_MergesMultipleLinesOnSameTask()
    {
        var taskId = Guid.NewGuid();
        var line1 = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectTaskId = taskId,
            LineType = PlanningLineType.Budget,
            Quantity = 1m,
            UnitCostLcy = 5m,
            UnitPrice = 10m,
            ExchangeRate = 1m
        };
        var line2 = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectTaskId = taskId,
            LineType = PlanningLineType.Billable,
            Quantity = 1m,
            UnitCostLcy = 0m,
            UnitPrice = 20m,
            ExchangeRate = 1m
        };
        line1.Recalculate();
        line2.Recalculate();

        var result = ProjectStatisticsCalculator.CalculateOwnAmounts([line1, line2], []);

        result[taskId].BudgetCostLcy.Should().Be(5m);
        result[taskId].BudgetPrice.Should().Be(10m);
        result[taskId].BillablePrice.Should().Be(20m);
    }
}
