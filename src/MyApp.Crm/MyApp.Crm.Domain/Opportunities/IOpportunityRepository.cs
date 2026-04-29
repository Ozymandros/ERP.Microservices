using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Opportunities;

/// <summary>
/// Defines the contract for I Opportunity Repository.
/// </summary>
public interface IOpportunityRepository : IRepository<Opportunity, Guid>
{
    Task<IEnumerable<Opportunity>> ListAsync();

    Task<List<Opportunity>> ListForForecastAsync(
        string ownerUsername,
        DateOnly? fromExpectedCloseDate,
        DateOnly? toExpectedCloseDate,
        CancellationToken cancellationToken = default);
}

