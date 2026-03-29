using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Contracts.Services;

public interface ILeadService
{
    Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeadDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResult<LeadDto>> ListPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedResult<LeadDto>> QueryAsync(ISpecification<Lead> spec, CancellationToken cancellationToken = default);

    Task<LeadDto> CreateAsync(CreateLeadDto dto, CancellationToken cancellationToken = default);
    Task<LeadDto> UpdateAsync(Guid id, UpdateLeadDto dto, CancellationToken cancellationToken = default);
    Task QualifyAsync(Guid id, QualifyLeadDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

