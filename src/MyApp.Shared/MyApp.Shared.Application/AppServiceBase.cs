using Microsoft.Extensions.Logging;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Shared.Application;

/// <summary>
/// Non-generic base class for application services. Centralizes the dependencies required
/// to publish audit records via Dapr and exposes a best-effort audit publisher that derived
/// services can invoke after a unit-of-work has been persisted.
/// </summary>
/// <remarks>
/// <para>
/// All application services in the solution should derive from this class (directly or via
/// the generic <see cref="AppServiceBase{T, TEntity, TEntityDto}"/>) so that audit publishing
/// is uniform across the system.
/// </para>
/// <para>
/// The Audit microservice itself MUST set <see cref="DisableAuditPublishing"/> in its
/// derived classes to avoid recursively publishing its own writes back to itself.
/// </para>
/// </remarks>
public abstract class AppServiceBase
{
    /// <summary>
    /// Gets the Dapr-based service invoker used to call the audit-service.
    /// </summary>
    protected IServiceInvoker ServiceInvoker { get; }

    /// <summary>
    /// Gets the logger associated with the derived service for audit-publish diagnostics.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// When <see langword="true"/>, <see cref="PublishAuditAsync"/> is a no-op. Derived
    /// services owned by the Audit microservice itself must override this and return true.
    /// </summary>
    protected virtual bool DisableAuditPublishing => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppServiceBase"/> class.
    /// </summary>
    /// <param name="serviceInvoker">Dapr-based service invoker used to call the audit-service.</param>
    /// <param name="logger">Logger used to record audit-publish warnings without failing the caller.</param>
    protected AppServiceBase(IServiceInvoker serviceInvoker, ILogger logger)
    {
        ServiceInvoker = serviceInvoker ?? throw new ArgumentNullException(nameof(serviceInvoker));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Best-effort publishes a collection of entity changes to the audit-service. One HTTP POST
    /// is issued per change. Failures are logged at Warning and swallowed so business
    /// operations continue to succeed even when the audit-service is unavailable.
    /// </summary>
    protected async Task PublishAuditAsync(
        IReadOnlyCollection<EntityEntryDto> changes,
        CancellationToken cancellationToken = default)
    {
        if (DisableAuditPublishing || changes.Count == 0)
            return;

        foreach (var change in changes)
        {
            var dto = MapToCreateEntityChangeDto(change);
            if (dto is null)
            {
                Logger.LogDebug(
                    "Skipping audit publish for {EntityName} {State}: EntityId is not a Guid",
                    change.EntityName, change.State);
                continue;
            }

            try
            {
                await ServiceInvoker.InvokeAsync<CreateEntityChangeDto, EntityChangeDto>(
                    ServiceNames.Audit,
                    ApiEndpoints.Audit.EntityChanges,
                    HttpMethod.Post,
                    dto,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Audit publish failed for {EntityName} {State} {EntityId}",
                    change.EntityName, change.State, change.EntityId);
            }
        }
    }

    private static CreateEntityChangeDto? MapToCreateEntityChangeDto(EntityEntryDto change)
    {
        var entityId = change.EntityId switch
        {
            Guid guid => guid,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => (Guid?)null
        };

        if (entityId is null || entityId == Guid.Empty)
            return null;

        var changeType = change.State switch
        {
            "Added" => ChangeTypeEnum.Created,
            "Modified" => ChangeTypeEnum.Updated,
            "Deleted" => ChangeTypeEnum.Deleted,
            _ => (ChangeTypeEnum?)null
        };

        if (changeType is null)
            return null;

        var propertyChanges = change.Properties
            .Select(p => new CreatePropertyChangeDto
            {
                PropertyName = p.PropertyName,
                OriginalValue = p.OldValue?.ToString(),
                NewValue = p.NewValue?.ToString()
            })
            .ToList();

        return new CreateEntityChangeDto
        {
            EntityName = change.EntityName,
            EntityId = entityId.Value,
            ChangeType = changeType.Value,
            OriginalValue = changeType == ChangeTypeEnum.Created ? null : change.OriginalValue,
            NewValue = changeType == ChangeTypeEnum.Deleted ? null : change.NewValue,
            PropertyChanges = propertyChanges
        };
    }
}

/// <summary>
/// Generic application service base bound to a single aggregate. Exposes a
/// <see cref="SaveChangesAsync"/> entry point that persists pending changes through the
/// injected repository and forwards the resulting <see cref="EntityEntryDto"/> collection to
/// <see cref="AppServiceBase.PublishAuditAsync"/>.
/// </summary>
/// <typeparam name="T">The primary key type for <typeparamref name="TEntity"/>.</typeparam>
/// <typeparam name="TEntity">The aggregate root entity type owned by the service.</typeparam>
/// <typeparam name="TEntityDto">The DTO used to expose <typeparamref name="TEntity"/> to callers.</typeparam>
public abstract class AppServiceBase<T, TEntity, TEntityDto> : AppServiceBase
    where TEntity : class, IEntity<T>
    where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    where TEntityDto : BaseDto<T>
{
    /// <summary>
    /// Gets the repository for the aggregate entity managed by this service.
    /// </summary>
    protected IRepository<TEntity, T> Repository { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppServiceBase{T, TEntity, TEntityDto}"/> class.
    /// </summary>
    protected AppServiceBase(
        IRepository<TEntity, T> repository,
        IServiceInvoker serviceInvoker,
        ILogger logger)
        : base(serviceInvoker, logger)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Persists pending repository changes and best-effort publishes each entity change
    /// to the audit-service. Audit failures are logged and swallowed.
    /// </summary>
    protected virtual async Task<IReadOnlyCollection<EntityEntryDto>> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var changes = await Repository.SaveChangesAsync(cancellationToken);
        await PublishAuditAsync(changes, cancellationToken);
        return changes;
    }
}
