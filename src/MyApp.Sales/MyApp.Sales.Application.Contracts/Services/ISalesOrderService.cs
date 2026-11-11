using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Contracts.Services;

/// <summary>Defines the contract for sales order management and commercial workflow operations.</summary>
public interface ISalesOrderService
{
    // Basic CRUD operations
    /// <summary>Returns the sales order with the given identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The unique identifier of the sales order.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The matching <see cref="SalesOrderDto"/>, or <see langword="null"/>.</returns>
    Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the sales order with the given order number, or <see langword="null"/> if not found.</summary>
    /// <param name="orderNumber">The order number to search for.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The matching <see cref="SalesOrderDto"/>, or <see langword="null"/>.</returns>
    Task<SalesOrderDto?> GetSalesOrderByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Returns all sales orders as an enumerable sequence.</summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>All <see cref="SalesOrderDto"/> records.</returns>
    Task<IEnumerable<SalesOrderDto>> ListSalesOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated list of sales orders.</summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A <see cref="PaginatedResult{T}"/> containing the requested page of sales orders.</returns>
    Task<PaginatedResult<SalesOrderDto>> ListSalesOrdersPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated list of sales orders that satisfy the given specification.</summary>
    /// <param name="spec">The specification defining filtering, sorting, and pagination criteria.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A <see cref="PaginatedResult{T}"/> containing matching sales orders.</returns>
    Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec, CancellationToken cancellationToken = default);

    /// <summary>Creates a new sales order from the given data.</summary>
    /// <param name="dto">The sales order data to create.</param>
    /// <returns>The created <see cref="SalesOrderDto"/> with generated identifier and audit fields.</returns>
    Task<SalesOrderDto> CreateSalesOrderAsync(CreateUpdateSalesOrderDto dto);

    /// <summary>Updates the sales order with the given identifier using the provided data.</summary>
    /// <param name="id">The unique identifier of the sales order to update.</param>
    /// <param name="dto">The updated sales order data.</param>
    /// <returns>The updated <see cref="SalesOrderDto"/>.</returns>
    Task<SalesOrderDto> UpdateSalesOrderAsync(Guid id, CreateUpdateSalesOrderDto dto);

    /// <summary>Deletes the sales order with the given identifier.</summary>
    /// <param name="id">The unique identifier of the sales order to delete.</param>
    Task DeleteSalesOrderAsync(Guid id);

    // Sales workflows
    /// <summary>Creates a quote with stock availability validation against the Inventory service.</summary>
    /// <param name="dto">The quote creation data including line items and validity period.</param>
    /// <returns>The created quote as a <see cref="SalesOrderDto"/> with <c>IsQuote</c> set to <see langword="true"/>.</returns>
    Task<SalesOrderDto> CreateQuoteAsync(CreateQuoteDto dto);

    /// <summary>Confirms a quote and converts it to a fulfillment order, triggering an outbound order via the Orders service.</summary>
    /// <param name="dto">The confirmation data including the quote identifier and warehouse.</param>
    /// <returns>The confirmed <see cref="SalesOrderDto"/> linked to the created fulfillment order.</returns>
    Task<SalesOrderDto> ConfirmQuoteAsync(ConfirmQuoteDto dto);

    /// <summary>Checks stock availability for the specified quote line items against the Inventory service.</summary>
    /// <param name="lines">The line items to check, each containing a product identifier and requested quantity.</param>
    /// <returns>A list of <see cref="StockAvailabilityCheckDto"/> with per-product availability results.</returns>
    Task<List<StockAvailabilityCheckDto>> CheckStockAvailabilityAsync(List<CreateUpdateSalesOrderLineDto> lines);

    /// <summary>Marks a sales order as shipped when its outbound operational order is fulfilled.</summary>
    Task<SalesOrderCacheInvalidationDto?> MarkShippedFromFulfillmentAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken = default);

    /// <summary>Marks a linked sales order as cancelled when its operational order is cancelled.</summary>
    Task<SalesOrderCacheInvalidationDto?> MarkCancelledFromOrderAsync(OrderCancelledEvent @event, CancellationToken cancellationToken = default);

    /// <summary>Marks a sales order as invoiced when billing issues an invoice for it.</summary>
    Task<SalesOrderCacheInvalidationDto?> MarkInvoicedFromBillingAsync(InvoiceIssuedEvent @event, CancellationToken cancellationToken = default);

    /// <summary>Creates an operational return order linked to an invoiced sales order.</summary>
    Task<SalesOrderDto> CreateReturnAsync(Guid salesOrderId, CreateSalesReturnDto dto, CancellationToken cancellationToken = default);
}
