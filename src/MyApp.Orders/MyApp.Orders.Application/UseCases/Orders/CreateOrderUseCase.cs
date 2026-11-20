using AutoMapper;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Application.UseCases.Orders;

/// <summary>Creates a draft operational order.</summary>
public sealed class CreateOrderUseCase(
    IOrderRepository orders,
    IMapper mapper,
    IOrdersTransactionBoundary boundary)
{
    public async Task<OrderDto> ExecuteAsync(CreateUpdateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var entity = mapper.Map<Order>(dto);
        entity.Id = Guid.NewGuid();
        entity.OrderDate = dto.OrderDate;
        entity.Status = OrderStatus.Draft;
        entity.OrderNumber = await OrderNumberGenerator.GenerateAsync(cancellationToken);

        foreach (var line in entity.Lines)
        {
            line.Id = Guid.NewGuid();
            line.OrderId = entity.Id;
        }

        await orders.AddAsync(entity);
        await boundary.SaveChangesAsync(cancellationToken);
        return mapper.Map<OrderDto>(entity);
    }
}