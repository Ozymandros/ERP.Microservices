using MyApp.Orders.Application.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApp.Orders.Application.Contracts
{
    public interface IOrderService
    {
        // Basic CRUD operations
        Task<OrderDto> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderDto>> ListAsync();
        Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto);
        Task UpdateAsync(Guid id, CreateUpdateOrderDto dto);
        Task DeleteAsync(Guid id);
        
        // Fulfillment workflows
        /// <summary>
        /// Creates an order with automatic stock reservation in the specified warehouse
        /// </summary>
        Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto dto);
        
        /// <summary>
        /// Fulfills an order by confirming reservations and creating inventory transactions
        /// </summary>
        Task<OrderDto> FulfillOrderAsync(FulfillOrderDto dto);
        
        /// <summary>
        /// Cancels an order and releases all stock reservations
        /// </summary>
        Task CancelOrderAsync(CancelOrderDto dto);
    }
}
