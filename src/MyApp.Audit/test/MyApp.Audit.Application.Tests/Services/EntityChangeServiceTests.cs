using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Application.Services;
using MyApp.Audit.Application.Tests.Common;
using MyApp.Audit.Domain;
using MyApp.Audit.Domain.Repositories;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Audit.Domain.Specifications;
using Xunit;

namespace MyApp.Audit.Application.Tests.Services;

public class EntityChangeServiceTests
{
    private readonly Mock<IEntityChangeRepository> _repository = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IServiceInvoker> _serviceInvoker = new();
    private readonly Mock<ILogger<EntityChangeService>> _logger = new();
    private readonly IMapper _mapper;
    private readonly EntityChangeService _sut;

    public EntityChangeServiceTests()
    {
        _mapper = MapperTestHelper.CreateMapper();
        _sut = new EntityChangeService(_repository.Object, _mapper, _cache.Object, _serviceInvoker.Object, _logger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedWithoutRepositoryCall()
    {
        var id = Guid.NewGuid();
        var cached = new EntityChangeDto(
            id, "Product", Guid.NewGuid(), "Created", null, null,
            DateTime.UtcNow, "user", null, null, []);

        _cache.Setup(c => c.GetStateAsync<EntityChangeDto>($"audit:entity-change:{id}"))
            .ReturnsAsync(cached);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeSameAs(cached);
        _repository.Verify(r => r.GetByIdWithPropertiesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_LoadsFromRepositoryAndCaches()
    {
        var id = Guid.NewGuid();
        var entity = new EntityChange(id)
        {
            EntityName = "Product",
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeTypeEnum.Created,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user"
        };

        _cache.Setup(c => c.GetStateAsync<EntityChangeDto>($"audit:entity-change:{id}"))
            .ReturnsAsync((EntityChangeDto?)null);
        _repository.Setup(r => r.GetByIdWithPropertiesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.ChangeType.Should().Be("Created");
        _cache.Verify(c => c.SaveStateAsync(
            $"audit:entity-change:{id}",
            It.IsAny<EntityChangeDto>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_PersistsAndInvalidatesEntityCache()
    {
        var entityId = Guid.NewGuid();
        var dto = new CreateEntityChangeDto
        {
            EntityName = "Order",
            EntityId = entityId,
            ChangeType = ChangeTypeEnum.Updated,
            PropertyChanges =
            [
                new CreatePropertyChangeDto { PropertyName = "Status", OriginalValue = "Draft", NewValue = "Submitted" }
            ]
        };

        EntityChange? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<EntityChange>()))
            .Callback<EntityChange>(e => saved = e)
            .ReturnsAsync((EntityChange e) => e);
        _repository.Setup(r => r.GetByIdWithPropertiesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var result = await _sut.RecordAsync(dto);

        result.EntityName.Should().Be("Order");
        result.PropertyChanges.Should().HaveCount(1);
        _cache.Verify(c => c.RemoveStateAsync($"audit:entity:Order:{entityId}"), Times.Once);
    }

    [Fact]
    public async Task QueryAsync_ReturnsMappedPaginatedResult()
    {
        var entity = new EntityChange(Guid.NewGuid())
        {
            EntityName = "Product",
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeTypeEnum.Deleted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user"
        };

        var page = new PaginatedResult<EntityChange>(
            [entity], 1, 10, 1);

        _repository.Setup(r => r.QueryAsync(It.IsAny<MyApp.Shared.Domain.Specifications.ISpecification<EntityChange>>()))
            .ReturnsAsync(page);

        var spec = new EntityChangeQuerySpec(new QuerySpec { Page = 1, PageSize = 10 });
        var result = await _sut.QueryAsync(spec);

        result.Items.Should().HaveCount(1);
        result.Items.First().ChangeType.Should().Be("Deleted");
    }
}
