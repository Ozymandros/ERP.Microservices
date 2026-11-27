using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

public class ResourceServiceTests
{
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IResourceGroupRepository> _groups = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ResourceService _sut;

    public ResourceServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _sut = new ResourceService(
            _resources.Object,
            _groups.Object,
            _mapper.Object,
            _uow.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<ResourceService>>());
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedResources()
    {
        var resource = new Resource(Guid.NewGuid()) { ResourceNo = "R0001", Name = "Consultant" };
        _resources.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([resource]);
        _mapper.Setup(m => m.Map<List<ResourceDto>>(It.IsAny<List<Resource>>()))
            .Returns([new ResourceDto(resource.Id) { ResourceNo = "R0001", Name = "Consultant" }]);

        var result = await _sut.ListAsync();

        result.Should().ContainSingle(r => r.ResourceNo == "R0001");
    }

    [Fact]
    public async Task QueryAsync_ReturnsPaginatedMappedDtos()
    {
        var resource = new Resource(Guid.NewGuid()) { ResourceNo = "R0002" };
        var spec = Mock.Of<ISpecification<Resource>>();
        var page = new PaginatedResult<Resource>([resource], 1, 10, 1);
        _resources.Setup(r => r.QueryAsync(spec, It.IsAny<CancellationToken>())).ReturnsAsync(page);
        _mapper.Setup(m => m.Map<List<ResourceDto>>(page.Items))
            .Returns([new ResourceDto(resource.Id) { ResourceNo = "R0002" }]);

        var result = await _sut.QueryAsync(spec);

        result.Items.Should().ContainSingle(r => r.ResourceNo == "R0002");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var resource = new Resource(id) { ResourceNo = "R0001", Name = "Alice" };
        _resources.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _mapper.Setup(m => m.Map<ResourceDto>(resource))
            .Returns(new ResourceDto(id) { ResourceNo = "R0001", Name = "Alice" });

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetByIdAsync_WithGroup_LoadsGroup()
    {
        var groupId = Guid.NewGuid();
        var group = new ResourceGroup(groupId) { Code = "CONS", Name = "Consultants" };
        var resource = new Resource(Guid.NewGuid())
        {
            ResourceNo = "R0001",
            ResourceGroupId = groupId
        };
        _resources.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _groups.Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _mapper.Setup(m => m.Map<ResourceDto>(resource))
            .Returns(new ResourceDto(resource.Id) { ResourceNo = "R0001", ResourceGroupCode = "CONS" });

        var result = await _sut.GetByIdAsync(resource.Id);

        result!.ResourceGroupCode.Should().Be("CONS");
        resource.ResourceGroup.Should().Be(group);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _resources.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_Valid_AddsResource()
    {
        var dto = ValidCreateDto("r0001");
        _resources.Setup(r => r.GetByNumberAsync("R0001", It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);
        _resources.Setup(r => r.AddAsync(It.IsAny<Resource>())).ReturnsAsync((Resource r) => r);
        _mapper.Setup(m => m.Map<ResourceDto>(It.IsAny<Resource>()))
            .Returns((Resource r) => new ResourceDto(r.Id)
            {
                ResourceNo = r.ResourceNo,
                Name = r.Name,
                Type = r.Type.ToString(),
                BaseUnitOfMeasure = r.BaseUnitOfMeasure,
                UnitCost = r.UnitCost,
                UnitPrice = r.UnitPrice
            });

        var result = await _sut.CreateAsync(dto);

        result.ResourceNo.Should().Be("R0001");
        result.Name.Should().Be("Consultant");
        _resources.Verify(r => r.AddAsync(It.Is<Resource>(x => x.ResourceNo == "R0001")), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNumber_Throws()
    {
        _resources.Setup(r => r.GetByNumberAsync("R0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource(Guid.NewGuid()) { ResourceNo = "R0001" });

        var act = () => _sut.CreateAsync(ValidCreateDto("R0001"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_MachineWithUser_Throws()
    {
        _resources.Setup(r => r.GetByNumberAsync("M0001", It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);
        var dto = ValidCreateDto("M0001") with { Type = ResourceType.Machine, UserId = Guid.NewGuid() };

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Machines cannot*");
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesFields()
    {
        var id = Guid.NewGuid();
        var resource = new Resource(id) { ResourceNo = "R0001", Name = "Old" };
        _resources.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _mapper.Setup(m => m.Map<ResourceDto>(resource))
            .Returns((Resource r) => new ResourceDto(r.Id) { ResourceNo = r.ResourceNo, Name = r.Name });

        var result = await _sut.UpdateAsync(id, ValidCreateDto("R0001") with { Name = "New" });

        result.Name.Should().Be("New");
        resource.Name.Should().Be("New");
    }

    [Fact]
    public async Task UpdateAsync_Missing_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _resources.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);

        var act = () => _sut.UpdateAsync(id, ValidCreateDto("R0001"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_NotInUse_Deletes()
    {
        var resource = new Resource(Guid.NewGuid()) { ResourceNo = "R0001" };
        _resources.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _resources.Setup(r => r.IsInUseAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _resources.Setup(r => r.DeleteAsync(resource)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(resource.Id);

        _resources.Verify(r => r.DeleteAsync(resource), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_InUse_Throws()
    {
        var resource = new Resource(Guid.NewGuid()) { ResourceNo = "R0001" };
        _resources.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _resources.Setup(r => r.IsInUseAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.DeleteAsync(resource.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*block it*");
    }

    [Fact]
    public async Task ListGroupsAsync_ReturnsOrderedMappedGroups()
    {
        var g1 = new ResourceGroup(Guid.NewGuid()) { Code = "B", Name = "Beta" };
        var g2 = new ResourceGroup(Guid.NewGuid()) { Code = "A", Name = "Alpha" };
        _groups.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([g1, g2]);
        _mapper.Setup(m => m.Map<List<ResourceGroupDto>>(It.IsAny<object>()))
            .Returns((object src) => ((IEnumerable<ResourceGroup>)src)
                .Select(g => new ResourceGroupDto(g.Id) { Code = g.Code, Name = g.Name }).ToList());

        var result = await _sut.ListGroupsAsync();

        result.Select(g => g.Code).Should().Equal("A", "B");
    }

    [Fact]
    public async Task GetGroupByIdAsync_Existing_ReturnsDto()
    {
        var group = new ResourceGroup(Guid.NewGuid()) { Code = "CONS", Name = "Consultants" };
        _groups.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _mapper.Setup(m => m.Map<ResourceGroupDto>(group))
            .Returns(new ResourceGroupDto(group.Id) { Code = "CONS", Name = "Consultants" });

        var result = await _sut.GetGroupByIdAsync(group.Id);

        result.Should().NotBeNull();
        result!.Code.Should().Be("CONS");
    }

    [Fact]
    public async Task CreateGroupAsync_Valid_AddsGroup()
    {
        _groups.Setup(r => r.GetByCodeAsync("CONS", It.IsAny<CancellationToken>())).ReturnsAsync((ResourceGroup?)null);
        _groups.Setup(r => r.AddAsync(It.IsAny<ResourceGroup>())).ReturnsAsync((ResourceGroup g) => g);
        _mapper.Setup(m => m.Map<ResourceGroupDto>(It.IsAny<ResourceGroup>()))
            .Returns((ResourceGroup g) => new ResourceGroupDto(g.Id) { Code = g.Code, Name = g.Name });

        var result = await _sut.CreateGroupAsync(new CreateUpdateResourceGroupDto { Code = "cons", Name = " Consultants " });

        result.Code.Should().Be("CONS");
        result.Name.Should().Be("Consultants");
    }

    [Fact]
    public async Task UpdateGroupAsync_Existing_UpdatesName()
    {
        var group = new ResourceGroup(Guid.NewGuid()) { Code = "CONS", Name = "Old" };
        _groups.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _mapper.Setup(m => m.Map<ResourceGroupDto>(group))
            .Returns((ResourceGroup g) => new ResourceGroupDto(g.Id) { Code = g.Code, Name = g.Name });

        var result = await _sut.UpdateGroupAsync(group.Id, new CreateUpdateResourceGroupDto { Code = "CONS", Name = "New" });

        result.Name.Should().Be("New");
    }

    [Fact]
    public async Task DeleteGroupAsync_NotInUse_Deletes()
    {
        var group = new ResourceGroup(Guid.NewGuid()) { Code = "CONS" };
        _groups.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _groups.Setup(r => r.IsInUseAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _groups.Setup(r => r.DeleteAsync(group)).Returns(Task.CompletedTask);

        await _sut.DeleteGroupAsync(group.Id);

        _groups.Verify(r => r.DeleteAsync(group), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_InUse_Throws()
    {
        var group = new ResourceGroup(Guid.NewGuid()) { Code = "CONS" };
        _groups.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _groups.Setup(r => r.IsInUseAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.DeleteGroupAsync(group.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static CreateUpdateResourceDto ValidCreateDto(string resourceNo) => new()
    {
        ResourceNo = resourceNo,
        Name = "Consultant",
        Type = ResourceType.Person,
        BaseUnitOfMeasure = "hour",
        UnitCost = 45m,
        UnitPrice = 90m,
        DailyCapacity = 8m
    };
}
