using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Services;

/// <summary>
/// Provides Lead Service functionality.
/// </summary>
public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<LeadService> _logger;
    private readonly IEventPublisher _eventPublisher;

    public LeadService(
        ILeadRepository leadRepository,
        IMapper mapper,
        ILogger<LeadService> logger,
        IEventPublisher eventPublisher)
    {
        _leadRepository = leadRepository;
        _mapper = mapper;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    /// <summary>Get By Id Async.</summary>
    public async Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id);
        return lead is null ? null : _mapper.Map<LeadDto>(lead);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<LeadDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var leads = await _leadRepository.ListAsync();
        return _mapper.Map<IEnumerable<LeadDto>>(leads);
    }

    /// <summary>List Paginated Async.</summary>
    public async Task<PaginatedResult<LeadDto>> ListPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await _leadRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var dtos = _mapper.Map<IEnumerable<LeadDto>>(result.Items);
        return new PaginatedResult<LeadDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Query Async.</summary>
    public async Task<PaginatedResult<LeadDto>> QueryAsync(ISpecification<Lead> spec, CancellationToken cancellationToken = default)
    {
        var result = await _leadRepository.QueryAsync(spec);
        var dtos = result.Items.Select(l => _mapper.Map<LeadDto>(l)).ToList();
        return new PaginatedResult<LeadDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Create Async.</summary>
    public async Task<LeadDto> CreateAsync(CreateLeadDto dto, CancellationToken cancellationToken = default)
    {
        var lead = new Lead(
            id: Guid.NewGuid(),
            title: dto.Title,
            ownerUsername: dto.OwnerUsername,
            source: dto.Source);

        lead.UpdateDetails(dto.Title, dto.Source, dto.ContactName, dto.ContactEmail, dto.ContactPhone);

        await _leadRepository.AddAsync(lead);

        try
        {
            var @event = new CrmLeadCreatedEvent(lead.Id, lead.Title, lead.OwnerUsername, lead.Source);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmLeadCreated, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmLeadCreatedEvent for Lead {LeadId}", lead.Id);
        }

        return _mapper.Map<LeadDto>(lead);
    }

    /// <summary>Update Async.</summary>
    public async Task<LeadDto> UpdateAsync(Guid id, UpdateLeadDto dto, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (lead is null) throw new InvalidOperationException($"Lead with ID {id} not found.");

        lead.UpdateDetails(dto.Title, dto.Source, dto.ContactName, dto.ContactEmail, dto.ContactPhone);
        await _leadRepository.UpdateAsync(lead);

        try
        {
            var @event = new CrmLeadUpdatedEvent(lead.Id, lead.Title, lead.OwnerUsername, lead.Source);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmLeadUpdated, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmLeadUpdatedEvent for Lead {LeadId}", lead.Id);
        }

        return _mapper.Map<LeadDto>(lead);
    }

    /// <summary>Qualify Async.</summary>
    public async Task QualifyAsync(Guid id, QualifyLeadDto dto, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (lead is null) throw new InvalidOperationException($"Lead with ID {id} not found.");

        lead.Qualify(dto.CustomerId);
        await _leadRepository.UpdateAsync(lead);

        try
        {
            var @event = new CrmLeadQualifiedEvent(lead.Id, lead.CustomerId!.Value);
            await _eventPublisher.PublishAsync(MessagingConstants.Topics.CrmLeadQualified, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CrmLeadQualifiedEvent for Lead {LeadId}", lead.Id);
        }

        // Opportunity creation happens in a dedicated endpoint/service in later increments.
    }

    /// <summary>Delete Async.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (lead is null) return;
        await _leadRepository.DeleteAsync(lead);
    }
}

