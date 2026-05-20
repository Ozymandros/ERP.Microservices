using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Application;

namespace MyApp.Shared.Tests.Infrastructure;

public class AppServiceBaseTests
{
    private readonly Mock<IRepository<TestEntity, Guid>> _repository = new();
    private readonly Mock<IServiceInvoker> _serviceInvoker = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly TestAppService _sut;

    public AppServiceBaseTests()
    {
        _sut = new TestAppService(_repository.Object, _serviceInvoker.Object, _logger.Object);
    }

    [Fact]
    public async Task SaveChangesAsync_PublishesOneAuditCallPerEntityEntry()
    {
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        const string createdNewJson = """{"Name":"Widget"}""";
        const string updatedOriginalJson = """{"Email":"old@x.com"}""";
        const string updatedNewJson = """{"Email":"new@x.com"}""";

        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("Product", entityId1, "Added",
                    new[] { new PropertyChangeEntryDto("Name", null, "Widget") },
                    OriginalValue: null,
                    NewValue: createdNewJson),
                new("Customer", entityId2, "Modified",
                    new[] { new PropertyChangeEntryDto("Email", "old@x.com", "new@x.com") },
                    OriginalValue: updatedOriginalJson,
                    NewValue: updatedNewJson)
            });

        var result = await _sut.SaveAsync();

        result.Should().HaveCount(2);
        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                ServiceNames.Audit,
                ApiEndpoints.Audit.EntityChanges,
                HttpMethod.Post,
                It.Is<CreateEntityChangeDto>(d =>
                    d.EntityName == "Product"
                    && d.EntityId == entityId1
                    && d.ChangeType == ChangeTypeEnum.Created
                    && d.OriginalValue == null
                    && d.NewValue == createdNewJson
                    && d.PropertyChanges.Count == 1
                    && d.PropertyChanges[0].PropertyName == "Name"
                    && d.PropertyChanges[0].NewValue == "Widget"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                ServiceNames.Audit,
                ApiEndpoints.Audit.EntityChanges,
                HttpMethod.Post,
                It.Is<CreateEntityChangeDto>(d =>
                    d.EntityName == "Customer"
                    && d.EntityId == entityId2
                    && d.ChangeType == ChangeTypeEnum.Updated
                    && d.OriginalValue == updatedOriginalJson
                    && d.NewValue == updatedNewJson),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_ForwardsEntitySnapshotJson_WithCorrectNullRulesByChangeType()
    {
        var createdId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        var updatedId = Guid.NewGuid();
        const string deletedOriginalJson = """{"Name":"Gone"}""";
        const string updatedOriginalJson = """{"Qty":1}""";
        const string updatedNewJson = """{"Qty":2}""";
        const string createdNewJson = """{"Name":"New"}""";

        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("Product", createdId, "Added", [],
                    OriginalValue: null, NewValue: createdNewJson),
                new("Product", deletedId, "Deleted", [],
                    OriginalValue: deletedOriginalJson, NewValue: null),
                new("Product", updatedId, "Modified", [],
                    OriginalValue: updatedOriginalJson, NewValue: updatedNewJson)
            });

        await _sut.SaveAsync();

        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                ServiceNames.Audit,
                ApiEndpoints.Audit.EntityChanges,
                HttpMethod.Post,
                It.Is<CreateEntityChangeDto>(d =>
                    d.EntityId == createdId
                    && d.ChangeType == ChangeTypeEnum.Created
                    && d.OriginalValue == null
                    && d.NewValue == createdNewJson),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                ServiceNames.Audit,
                ApiEndpoints.Audit.EntityChanges,
                HttpMethod.Post,
                It.Is<CreateEntityChangeDto>(d =>
                    d.EntityId == deletedId
                    && d.ChangeType == ChangeTypeEnum.Deleted
                    && d.OriginalValue == deletedOriginalJson
                    && d.NewValue == null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                ServiceNames.Audit,
                ApiEndpoints.Audit.EntityChanges,
                HttpMethod.Post,
                It.Is<CreateEntityChangeDto>(d =>
                    d.EntityId == updatedId
                    && d.ChangeType == ChangeTypeEnum.Updated
                    && d.OriginalValue == updatedOriginalJson
                    && d.NewValue == updatedNewJson),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_SkipsEntriesWithNonGuidEntityId()
    {
        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("LegacyEntity", 42, "Added", Array.Empty<PropertyChangeEntryDto>())
            });

        await _sut.SaveAsync();

        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<CreateEntityChangeDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_AuditFailure_DoesNotBubbleUp()
    {
        var entityId = Guid.NewGuid();

        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("Product", entityId, "Added", Array.Empty<PropertyChangeEntryDto>())
            });

        _serviceInvoker
            .Setup(s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<CreateEntityChangeDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("audit-service unreachable"));

        Func<Task> act = async () => await _sut.SaveAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_NoChanges_DoesNotInvokeAuditService()
    {
        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        await _sut.SaveAsync();

        _serviceInvoker.Verify(
            s => s.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<CreateEntityChangeDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public sealed class TestEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public sealed record TestEntityDto(Guid Id) : BaseDto<Guid>(Id);

    private sealed class TestAppService : AppServiceBase<Guid, TestEntity, TestEntityDto>
    {
        public TestAppService(
            IRepository<TestEntity, Guid> repository,
            IServiceInvoker serviceInvoker,
            ILogger logger)
            : base(repository, serviceInvoker, logger)
        {
        }

        public Task<IReadOnlyCollection<EntityEntryDto>> SaveAsync(CancellationToken ct = default)
            => SaveChangesAsync(ct);
    }
}
