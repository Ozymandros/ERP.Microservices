using MyApp.Shared.Domain.Repositories;

namespace MyApp.Audit.Domain.Repositories;

/// <summary>Repository for entity change audit records.</summary>
public interface IEntityChangeRepository : IRepository<EntityChange, Guid>
{
    Task<EntityChange?> GetByIdWithPropertiesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<EntityChange>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
