using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Contracts.Services;

public interface IOpportunityService
{
    Task<OpportunityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OpportunityDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResult<OpportunityDto>> QueryAsync(ISpecification<Opportunity> spec, CancellationToken cancellationToken = default);

    Task<OpportunityDto> CreateAsync(CreateOpportunityDto dto, CancellationToken cancellationToken = default);
    Task<OpportunityDto> UpdateForecastAsync(Guid id, UpdateOpportunityForecastDto dto, CancellationToken cancellationToken = default);
    Task<OpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto, CancellationToken cancellationToken = default);
    Task<OpportunityDto> MarkWonAsync(Guid id, MarkOpportunityWonRequest request, CancellationToken cancellationToken = default);
    Task<OpportunityDto> MarkLostAsync(Guid id, MarkOpportunityLostDto dto, CancellationToken cancellationToken = default);
}

