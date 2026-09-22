using MyApp.Orders.Application.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Orders.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Application.Contracts
{
/// <summary>Defines the contract for the operational order service.</summary>
public interface IOrderService
{
    // Basic CRUD operations
    /// <summary>Retrieves an order by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>The order DTO corresponding to the specified ID.</returns>
    Task<OrderDto> GetByIdAsync(Guid id);

    /// <summary>Retrieves an order by its order number.</summary>
    /// <param name="orderNumber">The order number to search for.</param>
    /// <returns>The matching order DTO, or null if not found.</returns>
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>Retrieves all orders.</summary>
    /// <returns>A collection of all order DTOs.</returns>
    Task<IEnumerable<OrderDto>> ListAsync();

    /// <summary>Creates a new operational order.</summary>
    /// <param name="dto">The data for the new order.</param>
    /// <returns>The created order DTO.</returns>
    Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto);

    /// <summary>Updates an existing operational order.</summary>
    /// <param name="id">The unique identifier of the order to update.</param>
    /// <param name="dto">The updated order data.</param>
    Task UpdateAsync(Guid id, CreateUpdateOrderDto dto);

    /// <summary>Deletes an order by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the order to delete.</param>
    Task DeleteAsync(Guid id);

        // Fulfillment workflows
        /// <summary>
        /// Creates an order with automatic stock reservation in the specified warehouse
        /// </summary>
        /// <param name="dto">The data for the new order including warehouse and line items.</param>
        /// <returns>The created order DTO with reservation details.</returns>
        Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto dto);

        /// <summary>
        /// Fulfills an order by confirming reservations and creating inventory transactions
        /// </summary>
        /// <param name="dto">The fulfillment details including warehouse and tracking information.</param>
        /// <returns>The fulfilled order DTO.</returns>
        Task<OrderDto> FulfillOrderAsync(FulfillOrderDto dto);

        /// <summary>
        /// Cancels an order and releases all stock reservations
        /// </summary>
        /// <param name="dto">The cancellation details including the order ID and reason.</param>
        Task CancelOrderAsync(CancelOrderDto dto);

        // Queries with specifications
        /// <summary>Queries orders based on a specification with filtering, sorting, and pagination.</summary>
        /// <param name="spec">The specification to apply for filtering, sorting, and pagination.</param>
        /// <returns>A paginated result of order DTOs matching the specification.</returns>
        Task<PaginatedResult<OrderDto>> QueryOrdersAsync(ISpecification<Order> spec);
    }
}
