using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Contracts.Services;

public interface IPurchaseOrderService
{
    // Basic CRUD operations
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);
    Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(Guid supplierId);
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status);
    Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreateUpdatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(Guid id, CreateUpdatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(Guid id, PurchaseOrderStatus status);
    Task DeletePurchaseOrderAsync(Guid id);
    
    // Purchasing workflows
    /// <summary>
    /// Approves a purchase order for processing
    /// </summary>
    Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(ApprovePurchaseOrderDto dto);
    
    /// <summary>
    /// Receives a purchase order and updates inventory
    /// </summary>
    Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(ReceivePurchaseOrderDto dto);
}
