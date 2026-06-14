using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Application.Contracts.Services;
using MyApp.Audit.Domain;
using MyApp.Audit.Domain.Repositories;
using MyApp.Audit.Application.Mapping;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Audit.Application.Services;

/// <summary>Application service for recording and querying audit trail entries.</summary>
public class EntityChangeService : AppServiceBase, IEntityChangeService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IEntityChangeRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<EntityChangeService> _logger;

    /// <inheritdoc />
    /// <remarks>
    /// The Audit microservice must never publish audit records to itself, otherwise it would
    /// enter an infinite recursion on every write. This override disables audit publishing
    /// for the service that backs the audit-service.
    /// </remarks>
    protected override bool DisableAuditPublishing => true;

    public EntityChangeService(
        IEntityChangeRepository repository,
        IMapper mapper,
        ICacheService cache,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<EntityChangeService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Audit)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EntityChangeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"audit:entity-change:{id}";
        var cached = await _cache.GetStateAsync<EntityChangeDto>(cacheKey);
        if (cached is not null)
            return cached;

        var entity = await _repository.GetByIdWithPropertiesAsync(id, cancellationToken);
        if (entity is null)
        {
            _logger.LogWarning("EntityChange {Id} not found", id);
            return null;
        }

        var dto = EnrichUpdatedPropertyChanges(_mapper.Map<EntityChangeDto>(entity), entity);
        await _cache.SaveStateAsync(cacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task<IReadOnlyList<EntityChangeDto>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"audit:entity:{entityName}:{entityId}";
        var cached = await _cache.GetStateAsync<List<EntityChangeDto>>(cacheKey);
        if (cached is not null)
            return cached;

        var entities = await _repository.GetByEntityAsync(entityName, entityId, cancellationToken);
        var dtos = entities.Select(e => EnrichUpdatedPropertyChanges(_mapper.Map<EntityChangeDto>(e), e)).ToList();
        await _cache.SaveStateAsync(cacheKey, dtos, CacheDuration);
        return dtos;
    }

    public async Task<PaginatedResult<EntityChangeDto>> QueryAsync(
        ISpecification<EntityChange> spec,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.QueryAsync(spec);
        var items = result.Items
            .Select(e => EnrichUpdatedPropertyChanges(_mapper.Map<EntityChangeDto>(e), e));
        return new PaginatedResult<EntityChangeDto>(
            items,
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }

    private static EntityChangeDto EnrichUpdatedPropertyChanges(EntityChangeDto dto, EntityChange entity)
    {
        if (entity.ChangeType != ChangeTypeEnum.Updated || dto.PropertyChanges.Count > 0)
            return dto;

        if (string.IsNullOrWhiteSpace(entity.OriginalValue) || string.IsNullOrWhiteSpace(entity.NewValue))
            return dto;

        var derived = SnapshotPropertyChangeDeriver.DeriveReadDtos(entity.OriginalValue, entity.NewValue);
        if (derived.Count == 0)
            return dto;

        return dto with { PropertyChanges = derived };
    }

    public async Task<EntityChangeDto> RecordAsync(CreateEntityChangeDto dto, CancellationToken cancellationToken = default)
    {
        var changeId = Guid.NewGuid();
        var entityChange = new EntityChange(changeId)
        {
            EntityName = dto.EntityName,
            EntityId = dto.EntityId,
            ChangeType = dto.ChangeType,
            OriginalValue = dto.OriginalValue,
            NewValue = dto.NewValue
        };

        foreach (var propertyChange in dto.PropertyChanges)
        {
            entityChange.PropertyChanges.Add(new PropertyChange(Guid.NewGuid())
            {
                EntityChangeId = changeId,
                PropertyName = propertyChange.PropertyName,
                OriginalValue = propertyChange.OriginalValue,
                NewValue = propertyChange.NewValue
            });
        }

        await _repository.AddAsync(entityChange);
        await SaveChangesAsync(cancellationToken);

        await _cache.RemoveStateAsync($"audit:entity:{dto.EntityName}:{dto.EntityId}");

        _logger.LogInformation(
            "Recorded {ChangeType} audit for {EntityType} {EntityId}",
            dto.ChangeType,
            dto.EntityName,
            dto.EntityId);

        var saved = await _repository.GetByIdWithPropertiesAsync(changeId, cancellationToken)
            ?? entityChange;

        return EnrichUpdatedPropertyChanges(_mapper.Map<EntityChangeDto>(saved), saved);
    }

    /// <inheritdoc />
    public async Task RecordFromEventAsync(
        EntityChangesSavedEvent @event,
        CancellationToken cancellationToken = default)
    {
        foreach (var payload in @event.Changes)
        {
            var dto = EntityChangeEventMapper.ToCreateDto(payload);
            if (dto is null)
            {
                _logger.LogDebug(
                    "Skipping audit ingest for {EntityName} {State} from {SourceService}",
                    payload.EntityName,
                    payload.State,
                    @event.SourceService);
                continue;
            }

            await RecordAsync(dto, cancellationToken);
        }
    }
}
