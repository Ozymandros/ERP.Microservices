using MyApp.Orders.Application.Internals;
using MyApp.Orders.Domain;

namespace MyApp.Orders.Application.UseCases.Orders;

/// <summary>Deletes an order.</summary>
public sealed class DeleteOrderUseCase(IOrderRepository orders, IOrdersTransactionBoundary boundary)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await orders.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return;

        await orders.DeleteAsync(entity);
        await boundary.SaveChangesAsync(cancellationToken);
    }
}