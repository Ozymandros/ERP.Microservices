using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Application;

namespace MyApp.Shared.Tests.Infrastructure;

public class AppServiceBaseTests
{
    private readonly Mock<IRepository<TestEntity, Guid>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly TestAppService _sut;

    public AppServiceBaseTests()
    {
        _sut = new TestAppService(_repository.Object, _unitOfWork.Object, _eventPublisher.Object, _logger.Object);
    }

    [Fact]
    public async Task SaveChangesAsync_PublishesOneAuditEventWithAllEntityEntries()
    {
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        const string createdNewJson = """{"Name":"Widget"}""";
        const string updatedOriginalJson = """{"Email":"old@x.com"}""";
        const string updatedNewJson = """{"Email":"new@x.com"}""";

        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
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
        _eventPublisher.Verify(
            e => e.PublishAsync(
                MessagingConstants.Topics.AuditEntityChangesSaved,
                It.Is<EntityChangesSavedEvent>(evt =>
                    evt.SourceService == ServiceNames.Inventory
                    && evt.Changes.Count == 2
                    && evt.Changes.Any(c =>
                        c.EntityName == "Product"
                        && Equals(c.EntityId, entityId1)
                        && c.State == "Added"
                        && c.OriginalValue == null
                        && c.NewValue == createdNewJson
                        && c.Properties.Count == 1
                        && c.Properties[0].PropertyName == "Name"
                        && c.Properties[0].NewValue!.ToString() == "Widget")
                    && evt.Changes.Any(c =>
                        c.EntityName == "Customer"
                        && Equals(c.EntityId, entityId2)
                        && c.State == "Modified"
                        && c.OriginalValue == updatedOriginalJson
                        && c.NewValue == updatedNewJson)),
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

        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
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

        _eventPublisher.Verify(
            e => e.PublishAsync(
                MessagingConstants.Topics.AuditEntityChangesSaved,
                It.Is<EntityChangesSavedEvent>(evt =>
                    evt.Changes.Any(c =>
                        Equals(c.EntityId, createdId)
                        && c.State == "Added"
                        && c.OriginalValue == null
                        && c.NewValue == createdNewJson)
                    && evt.Changes.Any(c =>
                        Equals(c.EntityId, deletedId)
                        && c.State == "Deleted"
                        && c.OriginalValue == deletedOriginalJson
                        && c.NewValue == null)
                    && evt.Changes.Any(c =>
                        Equals(c.EntityId, updatedId)
                        && c.State == "Modified"
                        && c.OriginalValue == updatedOriginalJson
                        && c.NewValue == updatedNewJson)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_SkipsEntriesWithNonGuidEntityId()
    {
        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("LegacyEntity", 42, "Added", Array.Empty<PropertyChangeEntryDto>())
            });

        await _sut.SaveAsync();

        _eventPublisher.Verify(
            e => e.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<EntityChangesSavedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_AuditFailure_DoesNotBubbleUp()
    {
        var entityId = Guid.NewGuid();

        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("Product", entityId, "Added", Array.Empty<PropertyChangeEntryDto>())
            });

        _eventPublisher
            .Setup(e => e.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<EntityChangesSavedEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("audit-service unreachable"));

        Func<Task> act = async () => await _sut.SaveAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_ExcludesPolicyEntities_FromAuditPublish()
    {
        var productId = Guid.NewGuid();

        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("RefreshToken", Guid.NewGuid(), "Added", Array.Empty<PropertyChangeEntryDto>()),
                new("AgentMemory", Guid.NewGuid(), "Added", Array.Empty<PropertyChangeEntryDto>()),
                new("AgentSession", Guid.NewGuid(), "Modified", Array.Empty<PropertyChangeEntryDto>()),
                new("EntityChange", Guid.NewGuid(), "Added", Array.Empty<PropertyChangeEntryDto>()),
                new("PropertyChange", Guid.NewGuid(), "Added", Array.Empty<PropertyChangeEntryDto>()),
                new("Product", productId, "Added", Array.Empty<PropertyChangeEntryDto>())
            });

        await _sut.SaveAsync();

        _eventPublisher.Verify(
            e => e.PublishAsync(
                MessagingConstants.Topics.AuditEntityChangesSaved,
                It.Is<EntityChangesSavedEvent>(evt =>
                    evt.Changes.Count == 1
                    && evt.Changes[0].EntityName == "Product"
                    && Equals(evt.Changes[0].EntityId, productId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_OnlyExcludedEntities_DoesNotPublishAuditEvent()
    {
        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityEntryDto>
            {
                new("RefreshToken", Guid.NewGuid(), "Added", Array.Empty<PropertyChangeEntryDto>())
            });

        await _sut.SaveAsync();

        _eventPublisher.Verify(
            e => e.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<EntityChangesSavedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_NoChanges_DoesNotPublishAuditEvent()
    {
        _unitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        await _sut.SaveAsync();

        _eventPublisher.Verify(
            e => e.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<EntityChangesSavedEvent>(),
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
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher,
            ILogger logger)
            : base(repository, unitOfWork, eventPublisher, logger, ServiceNames.Inventory)
        {
        }

        public Task<IReadOnlyCollection<EntityEntryDto>> SaveAsync(CancellationToken ct = default)
            => SaveChangesAsync(ct);
    }
}
