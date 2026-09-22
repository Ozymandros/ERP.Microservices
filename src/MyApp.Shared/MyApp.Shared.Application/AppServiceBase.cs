using Microsoft.Extensions.Logging;
using MyApp.Shared.Domain.Audit;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Shared.Application;

/// <summary>
/// Non-generic base class for application services. Commits via <see cref="IUnitOfWork"/>
/// and best-effort publishes entity-change audit events after successful persistence.
/// </summary>
/// <remarks>
/// The Audit microservice MUST set <see cref="DisableAuditPublishing"/> to avoid recursion.
/// <see cref="AuditExclusions"/> entity types (RefreshToken, AgentMemory, AgentSession, EntityChange, PropertyChange, etc.) are never published.
/// </remarks>
public abstract class AppServiceBase
{
    /// <summary>
    /// Gets the unit of work for the current service database scope.
    /// </summary>
    protected IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets the Dapr event publisher (audit uses topic <see cref="MessagingConstants.Topics.AuditEntityChangesSaved"/>).
    /// </summary>
    protected IEventPublisher EventPublisher { get; }

    /// <summary>
    /// Gets the logger for audit-publish diagnostics.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Dapr app-id of this microservice, included in audit events. May be null if unknown.
    /// </summary>
    protected string? SourceServiceName { get; }

    /// <summary>
    /// When <see langword="true"/>, audit publishing is a no-op (Audit microservice).
    /// </summary>
    protected virtual bool DisableAuditPublishing => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppServiceBase"/> class.
    /// </summary>
    protected AppServiceBase(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger logger,
        string? sourceServiceName = null)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SourceServiceName = sourceServiceName;
    }

    /// <summary>
    /// Commits pending unit-of-work changes and best-effort publishes an audit event.
    /// </summary>
    protected async Task<IReadOnlyCollection<EntityEntryDto>> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var changes = await UnitOfWork.CommitAsync(cancellationToken);
        await PublishEntityChangesAuditAsync(changes, cancellationToken);
        return changes;
    }

    /// <summary>
    /// Best-effort publishes entity changes to the audit topic. Failures are logged and swallowed.
    /// </summary>
    protected async Task PublishEntityChangesAuditAsync(
        IReadOnlyCollection<EntityEntryDto> changes,
        CancellationToken cancellationToken = default)
    {
        if (DisableAuditPublishing || changes.Count == 0)
            return;

        changes = AuditExclusions.FilterForAudit(changes, c => c.EntityName);
        if (changes.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(SourceServiceName))
        {
            Logger.LogWarning(
                "Skipping audit publish: {ChangeCount} changes but SourceServiceName is not configured",
                changes.Count);
            return;
        }

        var payloads = changes
            .Select(MapToPayload)
            .Where(p => p is not null)
            .Cast<EntityChangePayload>()
            .ToList();

        if (payloads.Count == 0)
        {
            Logger.LogWarning(
                "Skipping audit publish for {SourceService}: {TrackedCount} tracked changes produced no publishable payloads (check entity IDs are Guid)",
                SourceServiceName,
                changes.Count);
            return;
        }

        var evt = new EntityChangesSavedEvent(SourceServiceName, payloads);

        try
        {
            await EventPublisher.PublishAsync(
                MessagingConstants.Topics.AuditEntityChangesSaved,
                evt,
                cancellationToken);

            Logger.LogInformation(
                "Published audit event: topic={Topic} source={SourceService} changes={ChangeCount}",
                MessagingConstants.Topics.AuditEntityChangesSaved,
                SourceServiceName,
                payloads.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Audit event publish failed for {SourceService} with {ChangeCount} changes on topic {Topic}",
                SourceServiceName,
                payloads.Count,
                MessagingConstants.Topics.AuditEntityChangesSaved);
        }
    }

    private static EntityChangePayload? MapToPayload(EntityEntryDto change)
    {
        if (change.EntityId is null)
            return null;

        var resolvedId = ResolveEntityIdForAudit(change.EntityId);
        if (resolvedId is null)
            return null;

        var properties = change.Properties
            .Select(p => new PropertyChangePayload(
                p.PropertyName,
                p.OldValue,
                p.NewValue))
            .ToList();

        return new EntityChangePayload(
            change.EntityName,
            resolvedId,
            change.State,
            properties,
            change.OriginalValue,
            change.NewValue);
    }

    private static Guid? ResolveEntityIdForAudit(object? entityId) => entityId switch
    {
        Guid guid when guid != Guid.Empty => guid,
        string s when Guid.TryParse(s, out var parsed) && parsed != Guid.Empty => parsed,
        _ => null
    };
}

/// <summary>
/// Generic application service base bound to a single aggregate repository.
/// </summary>
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
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger logger,
        string? sourceServiceName = null)
        : base(unitOfWork, eventPublisher, logger, sourceServiceName)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
}
