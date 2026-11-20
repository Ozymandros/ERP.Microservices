using Microsoft.Extensions.Logging;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Billing.Application.Internals;

/// <summary>Unit-of-work commit with audit publishing for Billing use cases.</summary>
public interface IBillingTransactionBoundary
{
    /// <summary>Gets the Dapr event publisher.</summary>
    IEventPublisher EventPublisher { get; }

    /// <summary>Commits pending changes and publishes entity audit events.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class BillingTransactionBoundary(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<BillingTransactionBoundary> logger)
    : AppServiceBase(unitOfWork, eventPublisher, logger, ServiceNames.Billing), IBillingTransactionBoundary
{
    /// <inheritdoc />
    public new IEventPublisher EventPublisher => base.EventPublisher;

    /// <inheritdoc />
    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}