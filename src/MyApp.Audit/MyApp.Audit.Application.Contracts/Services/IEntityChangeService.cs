using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Audit.Application.Contracts.Services;

/// <summary>Application service contract for audit trail operations.</summary>
public interface IEntityChangeService
{
    /// <summary>
    /// Retrieves a single entity change record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity change record.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="EntityChangeDto"/>, or <see langword="null"/> if not found.</returns>
    Task<EntityChangeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audit records for a specific entity instance.
    /// </summary>
    /// <param name="entityName">The name of the entity type (e.g. <c>"Product"</c>).</param>
    /// <param name="entityId">The unique identifier of the entity instance.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of entity change records for the specified entity.</returns>
    Task<IReadOnlyList<EntityChangeDto>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a paginated query against the audit trail using the supplied specification.
    /// </summary>
    /// <param name="spec">The specification containing filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A paginated result containing the matching entity change records.</returns>
    Task<PaginatedResult<EntityChangeDto>> QueryAsync(
        ISpecification<EntityChange> spec,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new entity change record to the audit trail.
    /// </summary>
    /// <param name="dto">The data transfer object describing the entity change to record.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The persisted <see cref="EntityChangeDto"/> including assigned identifiers.</returns>
    Task<EntityChangeDto> RecordAsync(CreateEntityChangeDto dto, CancellationToken cancellationToken = default);

    /// <summary>Persists entity changes from a pub/sub audit event.</summary>
    /// <param name="event">The event payload containing one or more entity change payloads.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task RecordFromEventAsync(EntityChangesSavedEvent @event, CancellationToken cancellationToken = default);
}
