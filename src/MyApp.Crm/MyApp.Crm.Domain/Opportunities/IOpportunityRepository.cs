using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Opportunities;

/// <summary>
/// Defines the contract for I Opportunity Repository.
/// </summary>
public interface IOpportunityRepository : IRepository<Opportunity, Guid>
{
    /// <summary>Gets all opportunities.</summary>
    Task<IEnumerable<Opportunity>> ListAsync();

    /// <summary>Gets opportunities for a given owner filtered by expected close date range for forecast calculations.</summary>
    /// <param name="ownerUsername">The username of the opportunity owner to filter by.</param>
    /// <param name="fromExpectedCloseDate">The start of the expected close date range, or null for no lower bound.</param>
    /// <param name="toExpectedCloseDate">The end of the expected close date range, or null for no upper bound.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of matching opportunities.</returns>
    Task<List<Opportunity>> ListForForecastAsync(
        string ownerUsername,
        DateOnly? fromExpectedCloseDate,
        DateOnly? toExpectedCloseDate,
        CancellationToken cancellationToken = default);
}

