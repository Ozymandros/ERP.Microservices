using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Application.Contracts.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);
    Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(Guid supplierId);
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreateUpdatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(Guid id, CreateUpdatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(Guid id, PurchaseOrderStatus status);
    Task DeletePurchaseOrderAsync(Guid id);
}
