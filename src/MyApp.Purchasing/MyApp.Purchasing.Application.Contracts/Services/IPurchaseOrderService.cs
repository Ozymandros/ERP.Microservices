using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Contracts.Services;

/// <summary>Defines the contract for the purchase order service.</summary>
public interface IPurchaseOrderService
{
    // Basic CRUD operations
    /// <summary>Retrieves a purchase order by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the purchase order.</param>
    /// <returns>The purchase order DTO, or <c>null</c> if not found.</returns>
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);
    /// <summary>Retrieves a purchase order by its order number.</summary>
    /// <param name="orderNumber">The order number to look up.</param>
    /// <returns>The purchase order DTO, or <c>null</c> if not found.</returns>
    Task<PurchaseOrderDto?> GetPurchaseOrderByOrderNumberAsync(string orderNumber);
    /// <summary>Retrieves all purchase orders.</summary>
    /// <returns>A collection of all purchase order DTOs.</returns>
    Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
    /// <summary>Retrieves all purchase orders for the specified supplier.</summary>
    /// <param name="supplierId">The unique identifier of the supplier.</param>
    /// <returns>A collection of purchase order DTOs for the supplier.</returns>
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(Guid supplierId);
    /// <summary>Retrieves all purchase orders with the specified status.</summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>A collection of purchase order DTOs with the given status.</returns>
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status);
    /// <summary>Retrieves a paginated, filtered and sorted collection of purchase orders.</summary>
    /// <param name="spec">The specification that defines filtering, sorting, and paging.</param>
    /// <returns>A paginated result containing matching purchase order DTOs.</returns>
    Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec);
    /// <summary>Creates a new purchase order.</summary>
    /// <param name="dto">The data for the new purchase order.</param>
    /// <returns>The created purchase order DTO.</returns>
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreateUpdatePurchaseOrderDto dto);
    /// <summary>Updates an existing purchase order.</summary>
    /// <param name="id">The unique identifier of the purchase order to update.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>The updated purchase order DTO.</returns>
    Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(Guid id, CreateUpdatePurchaseOrderDto dto);
    /// <summary>Updates the status of an existing purchase order.</summary>
    /// <param name="id">The unique identifier of the purchase order.</param>
    /// <param name="status">The new status to set.</param>
    /// <returns>The updated purchase order DTO.</returns>
    Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(Guid id, PurchaseOrderStatus status);
    /// <summary>Deletes the purchase order with the specified identifier.</summary>
    /// <param name="id">The unique identifier of the purchase order to delete.</param>
    Task DeletePurchaseOrderAsync(Guid id);

    // Purchasing workflows
    /// <summary>Approves a purchase order for processing.</summary>
    /// <param name="dto">The approval details including the purchase order ID and optional notes.</param>
    /// <returns>The approved purchase order DTO.</returns>
    Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(ApprovePurchaseOrderDto dto);

    /// <summary>Receives a purchase order and creates a corresponding inbound operational order.</summary>
    /// <param name="dto">The receiving details including warehouse, received date, and per-line quantities.</param>
    /// <returns>The received purchase order DTO.</returns>
    Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(ReceivePurchaseOrderDto dto);
}
