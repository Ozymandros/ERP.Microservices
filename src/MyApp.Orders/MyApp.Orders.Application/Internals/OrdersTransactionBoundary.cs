using Microsoft.Extensions.Logging;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Orders.Application.Internals;

/// <summary>Unit-of-work commit with audit publishing for Orders use cases.</summary>
public interface IOrdersTransactionBoundary
{
    IEventPublisher EventPublisher { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class OrdersTransactionBoundary(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<OrdersTransactionBoundary> logger)
    : AppServiceBase(unitOfWork, eventPublisher, logger, ServiceNames.Orders), IOrdersTransactionBoundary
{
    public new IEventPublisher EventPublisher => base.EventPublisher;

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}