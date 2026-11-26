using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Application.UseCases.Orders;

/// <summary>Updates an operational order.</summary>
public sealed class UpdateOrderUseCase(IOrderRepository orders, IOrdersTransactionBoundary boundary)
{
    public async Task ExecuteAsync(Guid id, CreateUpdateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await orders.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return;

        existing.OrderNumber = dto.OrderNumber;
        existing.OrderDate = dto.OrderDate;
        existing.Type = dto.Type;
        existing.SourceId = dto.SourceId;
        existing.TargetId = dto.TargetId;
        existing.ExternalOrderId = dto.ExternalOrderId;
        existing.ExternalOrderSource = dto.ExternalOrderSource;

        existing.Lines.Clear();
        foreach (var l in dto.Lines)
        {
            existing.Lines.Add(new OrderLine(Guid.NewGuid())
            {
                OrderId = existing.Id,
                ProductId = l.ProductId,
                Quantity = l.Quantity
            });
        }

        await orders.UpdateAsync(existing);
        await boundary.SaveChangesAsync(cancellationToken);
    }
}