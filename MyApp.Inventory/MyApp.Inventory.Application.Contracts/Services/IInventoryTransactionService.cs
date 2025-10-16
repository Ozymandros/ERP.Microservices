using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IInventoryTransactionService
{
    Task<InventoryTransactionDto?> GetTransactionByIdAsync(Guid id);
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseIdAsync(Guid warehouseId);
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByTypeAsync(TransactionType transactionType);
    Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync();
    Task<InventoryTransactionDto> CreateTransactionAsync(CreateUpdateInventoryTransactionDto dto);
    Task<InventoryTransactionDto> UpdateTransactionAsync(Guid id, CreateUpdateInventoryTransactionDto dto);
    Task DeleteTransactionAsync(Guid id);
}
