using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Services;

public class OpportunityService : IOpportunityService
{
    private const string QuoteNumberPrefix = "Q-CRM";
    private const int QuoteNumberIdSuffixLength = 8;

    private readonly IOpportunityRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<OpportunityService> _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceInvoker _serviceInvoker;

    public OpportunityService(
        IOpportunityRepository repository,
        IMapper mapper,
        ILogger<OpportunityService> logger,
        IEventPublisher eventPublisher,
        IServiceInvoker serviceInvoker)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _serviceInvoker = serviceInvoker;
    }

    public async Task<OpportunityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<OpportunityDto>(entity);
    }

    public async Task<IEnumerable<OpportunityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync();
        return _mapper.Map<IEnumerable<OpportunityDto>>(list);
    }

    public async Task<PaginatedResult<OpportunityDto>> QueryAsync(ISpecification<Opportunity> spec, CancellationToken cancellationToken = default)
    {
        var result = await _repository.QueryAsync(spec);
        var dtos = result.Items.Select(o => _mapper.Map<OpportunityDto>(o)).ToList();
        return new PaginatedResult<OpportunityDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    public async Task<OpportunityDto> CreateAsync(CreateOpportunityDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Opportunity(Guid.NewGuid(), dto.CustomerId, dto.Name, dto.OwnerUsername, dto.LeadId);
        await _repository.AddAsync(entity);

        try
        {
            var @event = new CrmOpportunityCreatedEvent(entity.Id, entity.CustomerId, entity.Name, entity.OwnerUsername);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityCreated, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityCreatedEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }

    public async Task<OpportunityDto> UpdateForecastAsync(Guid id, UpdateOpportunityForecastDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        entity.UpdateForecast(dto.Probability, dto.ExpectedAmount, dto.ExpectedCloseDate);
        await _repository.UpdateAsync(entity);

        return _mapper.Map<OpportunityDto>(entity);
    }

    public async Task<OpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        if (!Enum.TryParse<OpportunityStage>(dto.Stage, ignoreCase: true, out var newStage))
            throw new ArgumentException("Invalid stage value.", nameof(dto.Stage));

        var oldStage = entity.Stage;
        entity.MoveToStage(newStage);
        await _repository.UpdateAsync(entity);

        try
        {
            var @event = new CrmOpportunityStageChangedEvent(entity.Id, oldStage.ToString(), entity.Stage.ToString());
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityStageChanged, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityStageChangedEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }

    public async Task<OpportunityDto> MarkWonAsync(Guid id, MarkOpportunityWonRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        // Idempotency: if already won, allow conversion step to be retried safely.
        if (entity.Stage != OpportunityStage.Won)
        {
            entity.MarkWon(request.Note);
        }

        if (request.ConvertToQuote)
        {
            if (request.Quote is null)
                throw new ArgumentException("Quote details are required when ConvertToQuote is true.", nameof(request));

            if (!entity.ConvertedSalesQuoteId.HasValue)
            {
                var orderNumber = GenerateQuoteNumber(entity.Id);
                var salesRequest = new CreateQuoteDto(
                    OrderNumber: orderNumber,
                    CustomerId: entity.CustomerId,
                    OrderDate: request.Quote.OrderDate ?? DateTime.UtcNow,
                    ValidityDays: request.Quote.ValidityDays,
                    Lines: request.Quote.Lines);

                var salesQuote = await _serviceInvoker.InvokeAsync<CreateQuoteDto, SalesOrderDto>(
                    ServiceNames.Sales,
                    ApiEndpoints.Sales.Quotes,
                    HttpMethod.Post,
                    salesRequest,
                    cancellationToken);

                entity.SetConvertedQuote(salesQuote.Id, salesQuote.OrderNumber);
            }
        }

        await _repository.UpdateAsync(entity);

        try
        {
            var @event = new CrmOpportunityWonEvent(entity.Id, entity.CustomerId);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityWon, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityWonEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }

    private static string GenerateQuoteNumber(Guid opportunityId)
    {
        var now = DateTime.UtcNow;
        var suffix = opportunityId.ToString("N")[..QuoteNumberIdSuffixLength];
        return $"{QuoteNumberPrefix}-{now:yyyyMMddHHmmss}-{suffix}";
    }

    public async Task<OpportunityDto> MarkLostAsync(Guid id, MarkOpportunityLostDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        entity.MarkLost(dto.Reason);
        await _repository.UpdateAsync(entity);

        try
        {
            var @event = new CrmOpportunityLostEvent(entity.Id, entity.CustomerId, dto.Reason);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityLost, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityLostEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }
}

