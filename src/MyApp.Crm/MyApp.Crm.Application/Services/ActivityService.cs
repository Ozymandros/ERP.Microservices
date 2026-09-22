using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Services;

/// <summary>
/// Provides Activity Service functionality.
/// </summary>
public class ActivityService : AppServiceBase, IActivityService
{
    private readonly IActivityRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ActivityService> _logger;

    /// <summary>Initializes a new instance of the ActivityService class.</summary>
    /// Initializes a new instance of the ActivityService class.
    /// <param name="repository">The repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    public ActivityService(
        IActivityRepository repository,
        IMapper mapper,
        ILogger<ActivityService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Crm)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;    }

    /// <summary>Get By Id Async.</summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<ActivityDto>(entity);
    }

    /// <summary>List Async.</summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IEnumerable<ActivityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync();
        return _mapper.Map<IEnumerable<ActivityDto>>(list);
    }

    /// <summary>Query Async.</summary>
    /// <param name="spec">The spec.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<PaginatedResult<ActivityDto>> QueryAsync(ISpecification<Activity> spec, CancellationToken cancellationToken = default)
    {
        var result = await _repository.QueryAsync(spec);
        var dtos = result.Items.Select(a => _mapper.Map<ActivityDto>(a)).ToList();
        return new PaginatedResult<ActivityDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Create Async.</summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
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
        await SaveChangesAsync(cancellationToken);

        try
        {
            var @event = new CrmActivityCreatedEvent(
                entity.Id,
                entity.Type.ToString(),
                entity.Subject,
                entity.DueAt,
                entity.AssignedToUsername);
            await EventPublisher.PublishAsync(MessagingConstants.Topics.CrmActivityCreated, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmActivityCreatedEvent for Activity {ActivityId}", entity.Id);
        }

        return _mapper.Map<ActivityDto>(entity);
    }

    /// <summary>Complete Async.</summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ActivityDto> CompleteAsync(Guid id, CompleteActivityDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new InvalidOperationException($"Activity with ID {id} not found.");

        entity.Complete(dto.Note);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);

        try
        {
            var @event = new CrmActivityCompletedEvent(entity.Id, entity.CompletedAt ?? DateTimeOffset.UtcNow);
            await EventPublisher.PublishAsync(MessagingConstants.Topics.CrmActivityCompleted, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmActivityCompletedEvent for Activity {ActivityId}", entity.Id);
        }

        return _mapper.Map<ActivityDto>(entity);
    }
}

