using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Infrastructure.Data;
using MyApp.Projects.Infrastructure.Data.Repositories;
using Xunit;

namespace MyApp.Projects.Infrastructure.Tests.Repositories;

public class ProjectRepositoryTests
{
    private static ProjectsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ProjectsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task ProjectRepository_GetByNumber_ReturnsProject()
    {
        using var ctx = CreateContext();
        var project = new Project(Guid.NewGuid())
        {
            ProjectNumber = "PRJ-2026-00001",
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "USD",
            Status = ProjectStatus.Open
        };
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();
        var sut = new ProjectRepository(ctx);

        var result = await sut.GetByNumberAsync("PRJ-2026-00001");

        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
    }

    [Fact]
    public async Task ProjectTaskRepository_GetByProject_ReturnsOrderedTasks()
    {
        using var ctx = CreateContext();
        var projectId = Guid.NewGuid();
        var t1 = new ProjectTask(Guid.NewGuid()) { ProjectId = projectId, TaskNo = "2000", SortOrder = 2 };
        var t2 = new ProjectTask(Guid.NewGuid()) { ProjectId = projectId, TaskNo = "1000", SortOrder = 1 };
        ctx.ProjectTasks.AddRange(t1, t2);
        await ctx.SaveChangesAsync();
        var sut = new ProjectTaskRepository(ctx);

        var result = await sut.GetByProjectAsync(projectId);

        result[0].TaskNo.Should().Be("1000");
    }

    [Fact]
    public async Task ProjectLedgerEntryRepository_GetMaxEntryNo_Increments()
    {
        using var ctx = CreateContext();
        var projectId = Guid.NewGuid();
        var entry = new ProjectLedgerEntry(Guid.NewGuid())
        {
            ProjectId = projectId,
            EntryNo = 3,
            EntryType = ProjectLedgerEntryType.Usage,
            PostingDate = DateTime.UtcNow.Date,
            Kind = ProjectLineKind.Expense,
            Description = "Test",
            Quantity = 1,
            UnitCostLcy = 10,
            UnitPrice = 10,
            CurrencyCode = "USD",
            ExchangeRate = 1
        };
        ctx.LedgerEntries.Add(entry);
        await ctx.SaveChangesAsync();
        var sut = new ProjectLedgerEntryRepository(ctx);

        var max = await sut.GetMaxEntryNoAsync(projectId);

        max.Should().Be(3);
    }

    [Fact]
    public async Task ProjectPlanningLineRepository_GetByProject_FiltersByTask()
    {
        using var ctx = CreateContext();
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = projectId,
            ProjectTaskId = taskId,
            LineNo = 10000,
            Kind = ProjectLineKind.Resource,
            Description = "Line",
            Quantity = 1,
            UnitCostLcy = 1,
            UnitPrice = 1,
            CurrencyCode = "USD",
            ExchangeRate = 1
        };
        ctx.PlanningLines.Add(line);
        await ctx.SaveChangesAsync();
        var sut = new ProjectPlanningLineRepository(ctx);

        var result = await sut.GetByProjectAsync(projectId, taskId);

        result.Should().ContainSingle(l => l.Id == line.Id);
    }
}