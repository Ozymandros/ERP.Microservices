using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Audit.Application.Contracts.Services;

/// <summary>Application service contract for audit trail operations.</summary>
public interface IEntityChangeService
{
    Task<EntityChangeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EntityChangeDto>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<EntityChangeDto>> QueryAsync(
        ISpecification<EntityChange> spec,
        CancellationToken cancellationToken = default);

    Task<EntityChangeDto> RecordAsync(CreateEntityChangeDto dto, CancellationToken cancellationToken = default);

    /// <summary>Persists entity changes from a pub/sub audit event.</summary>
    Task RecordFromEventAsync(EntityChangesSavedEvent @event, CancellationToken cancellationToken = default);
}
