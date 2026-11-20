using AutoMapper;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.UseCases.Orders;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Orders.Application.Services;

/// <summary>Thin façade for operational order operations.</summary>
public class OrderService(
    IOrderRepository orders,
    IMapper mapper,
    CreateOrderUseCase createOrder,
    UpdateOrderUseCase updateOrder,
    DeleteOrderUseCase deleteOrder,
    CreateOrderWithReservationUseCase createWithReservation,
    FulfillOrderUseCase fulfillOrder,
    CancelOrderUseCase cancelOrder) : IOrderService
{
    public Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto) =>
        createOrder.ExecuteAsync(dto);

    public Task UpdateAsync(Guid id, CreateUpdateOrderDto dto) =>
        updateOrder.ExecuteAsync(id, dto);

    public Task DeleteAsync(Guid id) =>
        deleteOrder.ExecuteAsync(id);

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await orders.GetByIdAsync(id, cancellationToken);
        return mapper.Map<OrderDto>(entity!);
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var entity = await orders.GetByOrderNumberAsync(orderNumber, cancellationToken);
        return entity is null ? null : mapper.Map<OrderDto>(entity);
    }

    public async Task<IEnumerable<OrderDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await orders.GetAllAsync(cancellationToken);
        return list.Select(o => mapper.Map<OrderDto>(o));
    }

    public Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto dto) =>
        createWithReservation.ExecuteAsync(dto);

    public Task<OrderDto> FulfillOrderAsync(FulfillOrderDto dto) =>
        fulfillOrder.ExecuteAsync(dto);

    public Task CancelOrderAsync(CancelOrderDto dto) =>
        cancelOrder.ExecuteAsync(dto);

    public async Task<PaginatedResult<OrderDto>> QueryOrdersAsync(
        ISpecification<Order> spec,
        CancellationToken cancellationToken = default)
    {
        var result = await orders.QueryAsync(spec, cancellationToken);
        return new PaginatedResult<OrderDto>(
            result.Items.Select(o => mapper.Map<OrderDto>(o)),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }
}