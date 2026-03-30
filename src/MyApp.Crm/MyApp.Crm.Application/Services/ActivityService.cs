using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ActivityService> _logger;
    private readonly IEventPublisher _eventPublisher;

    public ActivityService(
        IActivityRepository repository,
        IMapper mapper,
        ILogger<ActivityService> logger,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<ActivityDto>(entity);
    }

    public async Task<IEnumerable<ActivityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync();
        return _mapper.Map<IEnumerable<ActivityDto>>(list);
    }

    public async Task<PaginatedResult<ActivityDto>> QueryAsync(ISpecification<Activity> spec, CancellationToken cancellationToken = default)
    {
        var result = await _repository.QueryAsync(spec);
        var dtos = result.Items.Select(a => _mapper.Map<ActivityDto>(a)).ToList();
        return new PaginatedResult<ActivityDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    public async Task<ActivityDto> CreateAsync(CreateActivityDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ActivityType>(dto.Type, ignoreCase: true, out var type))
            throw new ArgumentException("Invalid activity type.", nameof(dto.Type));

        var entity = new Activity(
            Guid.NewGuid(),
            dto.Subject,
            type,
            dto.DueAt,
            dto.AssignedToUsername,
            dto.LeadId,
            dto.OpportunityId,
            dto.CustomerId);

        await _repository.AddAsync(entity);

        try
        {
            var @event = new CrmActivityCreatedEvent(
                entity.Id,
                entity.Type.ToString(),
                entity.Subject,
                entity.DueAt,
                entity.AssignedToUsername);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmActivityCreated, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmActivityCreatedEvent for Activity {ActivityId}", entity.Id);
        }

        return _mapper.Map<ActivityDto>(entity);
    }

    public async Task<ActivityDto> CompleteAsync(Guid id, CompleteActivityDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new InvalidOperationException($"Activity with ID {id} not found.");

        entity.Complete(dto.Note);
        await _repository.UpdateAsync(entity);

        try
        {
            var @event = new CrmActivityCompletedEvent(entity.Id, entity.CompletedAt ?? DateTimeOffset.UtcNow);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmActivityCompleted, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmActivityCompletedEvent for Activity {ActivityId}", entity.Id);
        }

        return _mapper.Map<ActivityDto>(entity);
    }
}

