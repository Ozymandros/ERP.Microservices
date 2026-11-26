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
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        _sut = new ProjectService(
            _projects.Object,
            Mock.Of<IProjectTaskRepository>(),
            Mock.Of<IProjectPlanningLineRepository>(),
            Mock.Of<IProjectLedgerEntryRepository>(),
            Mock.Of<ICurrencyService>(),
            Mock.Of<ISalesCustomerLookupPort>(),
            new ProjectsSettings(),
            _mapper.Object,
            uow.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<ProjectService>>());
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var project = new Project(id) { ProjectNumber = "PRJ-1", Description = "Demo" };
        var dto = new ProjectDto(id) { ProjectNumber = "PRJ-1", Description = "Demo" };
        _projects.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _mapper.Setup(m => m.Map<ProjectDto>(project)).Returns(dto);

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
}
