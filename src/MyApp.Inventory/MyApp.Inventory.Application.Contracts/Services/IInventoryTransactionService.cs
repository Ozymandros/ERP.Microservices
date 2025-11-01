using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IInventoryTransactionService
{
    Task<InventoryTransactionDto?> GetTransactionByIdAsync(Guid id);
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseIdAsync(Guid warehouseId);
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByTypeAsync(TransactionType transactionType);
    Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync();
    Task<PaginatedResult<InventoryTransactionDto>> GetAllTransactionsPaginatedAsync(int pageNumber, int pageSize);
    Task<InventoryTransactionDto> CreateTransactionAsync(CreateUpdateInventoryTransactionDto dto);
    Task<InventoryTransactionDto> UpdateTransactionAsync(Guid id, CreateUpdateInventoryTransactionDto dto);
    Task DeleteTransactionAsync(Guid id);
}
