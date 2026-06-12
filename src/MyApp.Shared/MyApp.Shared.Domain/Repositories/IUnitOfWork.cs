namespace MyApp.Shared.Domain.Repositories;

/// <summary>
/// Commits pending changes for a single service <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
/// within one microservice boundary. Returns entity change snapshots for audit publishing.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all tracked changes and returns summaries captured at commit time.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Change summaries for entities in Added, Modified, or Deleted state.</returns>
    Task<IReadOnlyCollection<EntityEntryDto>> CommitAsync(CancellationToken cancellationToken = default);
}
