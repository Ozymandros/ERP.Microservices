using MyApp.Shared.Domain.Repositories;

namespace MyApp.Audit.Domain.Repositories;

/// <summary>Repository for entity change audit records.</summary>
public interface IEntityChangeRepository : IRepository<EntityChange, Guid>
{
    /// <summary>
    /// Retrieves a single entity change record by its identifier, including its associated property changes.
    /// </summary>
    /// <param name="id">The unique identifier of the entity change record.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="EntityChange"/> with property changes loaded, or <see langword="null"/> if not found.</returns>
    Task<EntityChange?> GetByIdWithPropertiesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audit records for a specific entity instance, ordered by creation date descending.
    /// </summary>
    /// <param name="entityName">The name of the entity type (e.g. <c>"Product"</c>).</param>
    /// <param name="entityId">The unique identifier of the entity instance.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of entity change records for the specified entity.</returns>
    Task<List<EntityChange>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
