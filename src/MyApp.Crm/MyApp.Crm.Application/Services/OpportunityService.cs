using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Crm.Application.Services;

/// <summary>
/// Provides Opportunity Service functionality.
/// </summary>
public class OpportunityService : AppServiceBase, IOpportunityService
{
    private const string QuoteNumberPrefix = "Q-CRM";
    private const int QuoteNumberIdSuffixLength = 8;

    private readonly IOpportunityRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<OpportunityService> _logger;
    private readonly IServiceInvoker _serviceInvoker;

    public OpportunityService(
        IOpportunityRepository repository,
        IMapper mapper,
        ILogger<OpportunityService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IServiceInvoker serviceInvoker)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Crm)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;        _serviceInvoker = serviceInvoker;
    }

    /// <summary>Get By Id Async.</summary>
    public async Task<OpportunityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<OpportunityDto>(entity);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<OpportunityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync();
        return _mapper.Map<IEnumerable<OpportunityDto>>(list);
    }

    /// <summary>Query Async.</summary>
    public async Task<PaginatedResult<OpportunityDto>> QueryAsync(ISpecification<Opportunity> spec, CancellationToken cancellationToken = default)
    {
        var result = await _repository.QueryAsync(spec);
        var dtos = result.Items.Select(o => _mapper.Map<OpportunityDto>(o)).ToList();
        return new PaginatedResult<OpportunityDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Create Async.</summary>
    public async Task<OpportunityDto> CreateAsync(CreateOpportunityDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Opportunity(Guid.NewGuid(), dto.CustomerId, dto.Name, dto.OwnerUsername, dto.LeadId);
        await _repository.AddAsync(entity);
        await SaveChangesAsync(cancellationToken);

        try
        {
            var @event = new CrmOpportunityCreatedEvent(entity.Id, entity.CustomerId, entity.Name, entity.OwnerUsername);
            await EventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityCreated, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityCreatedEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }

    /// <summary>Update Forecast Async.</summary>
    public async Task<OpportunityDto> UpdateForecastAsync(Guid id, UpdateOpportunityForecastDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        entity.UpdateForecast(dto.Probability, dto.ExpectedAmount, dto.ExpectedCloseDate);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);

        return _mapper.Map<OpportunityDto>(entity);
    }

    /// <summary>Move Stage Async.</summary>
    public async Task<OpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        if (!Enum.TryParse<OpportunityStage>(dto.Stage, ignoreCase: true, out var newStage))
            throw new ArgumentException("Invalid stage value.", nameof(dto.Stage));

        var oldStage = entity.Stage;
        entity.MoveToStage(newStage);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);

        try
        {
            var @event = new CrmOpportunityStageChangedEvent(entity.Id, oldStage.ToString(), entity.Stage.ToString());
            await EventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityStageChanged, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityStageChangedEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }

    /// <summary>Mark Won Async.</summary>
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

                var lines = request.Quote.Lines;
                if (lines is null || lines.Count == 0)
                {
                    lines = BuildQuoteLinesFromOpportunity(entity);
                }

                var salesRequest = new CreateQuoteDto(
                    OrderNumber: orderNumber,
                    CustomerId: entity.CustomerId,
                    OrderDate: request.Quote.OrderDate ?? DateTime.UtcNow,
                    ValidityDays: request.Quote.ValidityDays,
                    Lines: lines);

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
        await SaveChangesAsync(cancellationToken);

        try
        {
            var @event = new CrmOpportunityWonEvent(entity.Id, entity.CustomerId);
            await EventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityWon, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityWonEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }

    /// <summary>Add Line Async.</summary>
    public async Task<OpportunityLineDto> AddLineAsync(Guid opportunityId, CreateOpportunityLineDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(opportunityId);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {opportunityId} not found.");

        var line = entity.AddLine(
            Guid.NewGuid(),
            dto.Description,
            dto.Quantity,
            dto.UnitPrice,
            dto.DiscountPercent,
            dto.ProductId,
            dto.Sku);

        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<OpportunityLineDto>(line);
    }

    /// <summary>Update Line Async.</summary>
    public async Task<OpportunityLineDto> UpdateLineAsync(Guid opportunityId, Guid lineId, UpdateOpportunityLineDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(opportunityId);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {opportunityId} not found.");

        entity.UpdateLine(lineId, dto.Description, dto.Quantity, dto.UnitPrice, dto.DiscountPercent, dto.ProductId, dto.Sku);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);

        var updated = entity.Lines.First(l => l.Id == lineId);
        return _mapper.Map<OpportunityLineDto>(updated);
    }

    /// <summary>Remove Line Async.</summary>
    public async Task RemoveLineAsync(Guid opportunityId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(opportunityId);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {opportunityId} not found.");

        entity.RemoveLine(lineId);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>Get Forecast Summary Async.</summary>
    public async Task<ForecastSummaryDto> GetForecastSummaryAsync(
        string ownerUsername,
        DateOnly? fromExpectedCloseDate,
        DateOnly? toExpectedCloseDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerUsername))
            throw new ArgumentException("OwnerUsername is required.", nameof(ownerUsername));

        var list = await _repository.ListForForecastAsync(ownerUsername.Trim(), fromExpectedCloseDate, toExpectedCloseDate, cancellationToken);

        var byStage = list
            .GroupBy(o => o.Stage)
            .Select(g =>
            {
                var sumExpected = g.Sum(x => x.ExpectedAmount ?? 0m);
                var weighted = g.Sum(x => (x.ExpectedAmount ?? 0m) * x.Probability);
                return new ForecastByStageDto(g.Key.ToString(), g.Count(), sumExpected, weighted);
            })
            .OrderByDescending(x => x.WeightedAmount)
            .ToList();

        var totalExpected = list.Sum(x => x.ExpectedAmount ?? 0m);
        var totalWeighted = list.Sum(x => (x.ExpectedAmount ?? 0m) * x.Probability);

        return new ForecastSummaryDto(
            OwnerUsername: ownerUsername.Trim(),
            FromExpectedCloseDate: fromExpectedCloseDate,
            ToExpectedCloseDate: toExpectedCloseDate,
            TotalCount: list.Count,
            TotalExpectedAmount: totalExpected,
            TotalWeightedAmount: totalWeighted,
            ByStage: byStage);
    }

    private static string GenerateQuoteNumber(Guid opportunityId)
    {
        var now = DateTime.UtcNow;
        var suffix = opportunityId.ToString("N")[..QuoteNumberIdSuffixLength];
        return $"{QuoteNumberPrefix}-{now:yyyyMMddHHmmss}-{suffix}";
    }

    private static List<CreateUpdateSalesOrderLineDto> BuildQuoteLinesFromOpportunity(Opportunity entity)
    {
        if (entity.Lines.Count == 0)
            throw new ArgumentException("Quote lines are required. Either provide Quote.Lines or add opportunity lines before conversion.");

        var lines = new List<CreateUpdateSalesOrderLineDto>(entity.Lines.Count);
        foreach (var l in entity.Lines)
        {
            if (!l.ProductId.HasValue)
                throw new ArgumentException("All opportunity lines must have ProductId to convert to a Sales quote.");

            if (decimal.Truncate(l.Quantity) != l.Quantity)
                throw new ArgumentException("Opportunity line quantity must be a whole number to convert to a Sales quote.");

            if (l.Quantity > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(entity), "Opportunity line quantity is too large to convert.");

            lines.Add(new CreateUpdateSalesOrderLineDto(
                ProductId: l.ProductId.Value,
                Quantity: (int)l.Quantity,
                UnitPrice: l.UnitPrice));
        }

        return lines;
    }

    /// <summary>Mark Lost Async.</summary>
    public async Task<OpportunityDto> MarkLostAsync(Guid id, MarkOpportunityLostDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        entity.MarkLost(dto.Reason);
        await _repository.UpdateAsync(entity);
        await SaveChangesAsync(cancellationToken);

        try
        {
            var @event = new CrmOpportunityLostEvent(entity.Id, entity.CustomerId, dto.Reason);
            await EventPublisher.PublishAsync(MessagingConstants.Topics.CrmOpportunityLost, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmOpportunityLostEvent for Opportunity {OpportunityId}", entity.Id);
        }

        return _mapper.Map<OpportunityDto>(entity);
    }
}

