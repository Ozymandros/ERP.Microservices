using Microsoft.Extensions.Logging;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Purchasing.Application.Internals;

public interface IPurchasingTransactionBoundary
{
    IEventPublisher EventPublisher { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class PurchasingTransactionBoundary(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<PurchasingTransactionBoundary> logger)
    : AppServiceBase(unitOfWork, eventPublisher, logger, ServiceNames.Purchasing), IPurchasingTransactionBoundary
{
    public new IEventPublisher EventPublisher => base.EventPublisher;

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}