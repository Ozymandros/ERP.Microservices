using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Contracts.Services;

public interface IActivityService
{
    Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResult<ActivityDto>> QueryAsync(ISpecification<Activity> spec, CancellationToken cancellationToken = default);

    Task<ActivityDto> CreateAsync(CreateActivityDto dto, CancellationToken cancellationToken = default);
    Task<ActivityDto> CompleteAsync(Guid id, CompleteActivityDto dto, CancellationToken cancellationToken = default);
}

