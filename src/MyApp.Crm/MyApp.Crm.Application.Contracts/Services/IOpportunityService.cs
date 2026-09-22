using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Opportunity Service.
/// </summary>
public interface IOpportunityService
{
    /// <summary>Gets an opportunity by its unique identifier.</summary>
    /// <param name="id">The opportunity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The opportunity DTO, or null if not found.</returns>
    Task<OpportunityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all opportunities.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of all opportunity DTOs.</returns>
    Task<IEnumerable<OpportunityDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Queries opportunities using the specified specification.</summary>
    /// <param name="spec">The specification containing filters and pagination.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated result of matching opportunity DTOs.</returns>
    Task<PaginatedResult<OpportunityDto>> QueryAsync(ISpecification<Opportunity> spec, CancellationToken cancellationToken = default);

    /// <summary>Creates a new opportunity.</summary>
    /// <param name="dto">The data for the new opportunity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created opportunity DTO.</returns>
    Task<OpportunityDto> CreateAsync(CreateOpportunityDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates the forecast details of an opportunity.</summary>
    /// <param name="id">The opportunity ID.</param>
    /// <param name="dto">The updated forecast data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated opportunity DTO.</returns>
    Task<OpportunityDto> UpdateForecastAsync(Guid id, UpdateOpportunityForecastDto dto, CancellationToken cancellationToken = default);

    /// <summary>Moves an opportunity to a new pipeline stage.</summary>
    /// <param name="id">The opportunity ID.</param>
    /// <param name="dto">The data containing the target stage.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated opportunity DTO.</returns>
    Task<OpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto, CancellationToken cancellationToken = default);

    /// <summary>Marks an opportunity as won, optionally converting it to a sales quote.</summary>
    /// <param name="id">The opportunity ID.</param>
    /// <param name="request">The request containing win note and optional quote conversion data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated opportunity DTO.</returns>
    Task<OpportunityDto> MarkWonAsync(Guid id, MarkOpportunityWonRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marks an opportunity as lost with a reason.</summary>
    /// <param name="id">The opportunity ID.</param>
    /// <param name="dto">The DTO containing the reason for losing.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated opportunity DTO.</returns>
    Task<OpportunityDto> MarkLostAsync(Guid id, MarkOpportunityLostDto dto, CancellationToken cancellationToken = default);

    /// <summary>Adds a line item to an opportunity.</summary>
    /// <param name="opportunityId">The opportunity ID.</param>
    /// <param name="dto">The line item data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created opportunity line DTO.</returns>
    Task<OpportunityLineDto> AddLineAsync(Guid opportunityId, CreateOpportunityLineDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates a line item in an opportunity.</summary>
    /// <param name="opportunityId">The opportunity ID.</param>
    /// <param name="lineId">The line item ID.</param>
    /// <param name="dto">The updated line item data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated opportunity line DTO.</returns>
    Task<OpportunityLineDto> UpdateLineAsync(Guid opportunityId, Guid lineId, UpdateOpportunityLineDto dto, CancellationToken cancellationToken = default);

    /// <summary>Removes a line item from an opportunity.</summary>
    /// <param name="opportunityId">The opportunity ID.</param>
    /// <param name="lineId">The line item ID to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RemoveLineAsync(Guid opportunityId, Guid lineId, CancellationToken cancellationToken = default);

    /// <summary>Gets a forecast summary for a given owner and optional date range.</summary>
    /// <param name="ownerUsername">The username of the opportunity owner.</param>
    /// <param name="fromExpectedCloseDate">The start of the expected close date range, or null for no lower bound.</param>
    /// <param name="toExpectedCloseDate">The end of the expected close date range, or null for no upper bound.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A forecast summary DTO with totals and breakdown by stage.</returns>
    Task<ForecastSummaryDto> GetForecastSummaryAsync(
        string ownerUsername,
        DateOnly? fromExpectedCloseDate,
        DateOnly? toExpectedCloseDate,
        CancellationToken cancellationToken = default);
}

