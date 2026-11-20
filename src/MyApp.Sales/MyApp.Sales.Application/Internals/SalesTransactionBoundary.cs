using Microsoft.Extensions.Logging;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Sales.Application.Internals;

public interface ISalesTransactionBoundary
{
    IEventPublisher EventPublisher { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class SalesTransactionBoundary(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<SalesTransactionBoundary> logger)
    : AppServiceBase(unitOfWork, eventPublisher, logger, ServiceNames.Sales), ISalesTransactionBoundary
{
    public new IEventPublisher EventPublisher => base.EventPublisher;

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}