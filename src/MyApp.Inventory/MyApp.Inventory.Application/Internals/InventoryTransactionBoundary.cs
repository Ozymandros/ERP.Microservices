using Microsoft.Extensions.Logging;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Inventory.Application.Internals;

public interface IInventoryTransactionBoundary
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class InventoryTransactionBoundary(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<InventoryTransactionBoundary> logger)
    : AppServiceBase(unitOfWork, eventPublisher, logger, ServiceNames.Inventory), IInventoryTransactionBoundary
{
    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}