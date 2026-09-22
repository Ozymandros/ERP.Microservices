using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Activity Service.
/// </summary>
public interface IActivityService
{
    /// <summary>Gets an activity by its unique identifier.</summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The activity DTO, or null if not found.</returns>
    Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all activities.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of all activity DTOs.</returns>
    Task<IEnumerable<ActivityDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Queries activities using the specified specification.</summary>
    /// <param name="spec">The specification containing filters and pagination.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated result of matching activity DTOs.</returns>
    Task<PaginatedResult<ActivityDto>> QueryAsync(ISpecification<Activity> spec, CancellationToken cancellationToken = default);

    /// <summary>Creates a new activity.</summary>
    /// <param name="dto">The data for the new activity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created activity DTO.</returns>
    Task<ActivityDto> CreateAsync(CreateActivityDto dto, CancellationToken cancellationToken = default);

    /// <summary>Marks an activity as completed.</summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="dto">Optional completion note data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated activity DTO.</returns>
    Task<ActivityDto> CompleteAsync(Guid id, CompleteActivityDto dto, CancellationToken cancellationToken = default);
}

