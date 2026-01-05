using AutoMapper;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseOrderLineRepository _lineRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IMapper _mapper;

    public PurchaseOrderService(
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseOrderLineRepository lineRepository,
        ISupplierRepository supplierRepository,
        IMapper mapper)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _lineRepository = lineRepository;
        _supplierRepository = supplierRepository;
        _mapper = mapper;
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id);
        return order == null ? null : _mapper.Map<PurchaseOrderDto>(order);
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
    {
        var orders = await _purchaseOrderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(Guid supplierId)
    {
        var orders = await _purchaseOrderRepository.GetBySuppliersIdAsync(supplierId);
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status)
    {
        var orders = await _purchaseOrderRepository.GetByStatusAsync(status);
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreateUpdatePurchaseOrderDto dto)
    {
        // Validate supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{dto.SupplierId}' not found.");
        }

        var order = _mapper.Map<PurchaseOrder>(dto);
        order.Status = (PurchaseOrderStatus)dto.Status;

        // Calculate total amount from lines
        if (order.Lines.Any())
        {
            order.TotalAmount = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        }

        var createdOrder = await _purchaseOrderRepository.AddAsync(order);
        return _mapper.Map<PurchaseOrderDto>(createdOrder);
    }

    public async Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(Guid id, CreateUpdatePurchaseOrderDto dto)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{id}' not found.");
        }

        // Validate supplier exists if changed
        if (order.SupplierId != dto.SupplierId)
        {
            var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
            if (supplier == null)
            {
                throw new KeyNotFoundException($"Supplier with ID '{dto.SupplierId}' not found.");
            }
        }

        _mapper.Map(dto, order);
        order.Status = (PurchaseOrderStatus)dto.Status;

        // Recalculate total amount from lines
        if (order.Lines.Any())
        {
            order.TotalAmount = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        }

        var updatedOrder = await _purchaseOrderRepository.UpdateAsync(order);
        return _mapper.Map<PurchaseOrderDto>(updatedOrder);
    }

    public async Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(Guid id, PurchaseOrderStatus status)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{id}' not found.");
        }

        order.Status = status;
        var updatedOrder = await _purchaseOrderRepository.UpdateAsync(order);

        return _mapper.Map<PurchaseOrderDto>(updatedOrder);
    }

    public async Task DeletePurchaseOrderAsync(Guid id)
    {
        var order = await _purchaseOrderRepository.GetByIdAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{id}' not found.");
        }

        await _purchaseOrderRepository.DeleteAsync(order);
    }

    /// <summary>
    /// Query purchase orders with filtering, sorting, and pagination
    /// </summary>
    public async Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec)
    {
        var result = await _purchaseOrderRepository.QueryAsync(spec);
        var dtos = result.Items.Select(po => _mapper.Map<PurchaseOrderDto>(po)).ToList();
        return new PaginatedResult<PurchaseOrderDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
