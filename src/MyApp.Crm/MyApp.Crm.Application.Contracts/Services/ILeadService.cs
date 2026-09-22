using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Lead Service.
/// </summary>
public interface ILeadService
{
    /// <summary>Gets a lead by its unique identifier.</summary>
    /// <param name="id">The lead ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The lead DTO, or null if not found.</returns>
    Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all leads.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of all lead DTOs.</returns>
    Task<IEnumerable<LeadDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a paginated list of leads.</summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated result of lead DTOs.</returns>
    Task<PaginatedResult<LeadDto>> ListPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Queries leads using the specified specification.</summary>
    /// <param name="spec">The specification containing filters and pagination.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated result of matching lead DTOs.</returns>
    Task<PaginatedResult<LeadDto>> QueryAsync(ISpecification<Lead> spec, CancellationToken cancellationToken = default);

    /// <summary>Creates a new lead.</summary>
    /// <param name="dto">The data for the new lead.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created lead DTO.</returns>
    Task<LeadDto> CreateAsync(CreateLeadDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing lead's details.</summary>
    /// <param name="id">The lead ID.</param>
    /// <param name="dto">The updated lead data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated lead DTO.</returns>
    Task<LeadDto> UpdateAsync(Guid id, UpdateLeadDto dto, CancellationToken cancellationToken = default);

    /// <summary>Qualifies a lead and associates it with a customer.</summary>
    /// <param name="id">The lead ID.</param>
    /// <param name="dto">The qualification data including the customer ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task QualifyAsync(Guid id, QualifyLeadDto dto, CancellationToken cancellationToken = default);

    /// <summary>Deletes a lead by its unique identifier.</summary>
    /// <param name="id">The lead ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

