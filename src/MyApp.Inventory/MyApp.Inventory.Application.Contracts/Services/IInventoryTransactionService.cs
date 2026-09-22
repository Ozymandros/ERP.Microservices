using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Inventory Transaction Service.
/// </summary>
public interface IInventoryTransactionService
{
    /// <summary>Retrieves an inventory transaction by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the transaction.</param>
    /// <returns>The matching <see cref="InventoryTransactionDto"/>, or <c>null</c> if not found.</returns>
    Task<InventoryTransactionDto?> GetTransactionByIdAsync(Guid id);

    /// <summary>Retrieves an inventory transaction by its external reference number.</summary>
    /// <param name="referenceNumber">The reference number to search for.</param>
    /// <returns>The matching <see cref="InventoryTransactionDto"/>, or <c>null</c> if not found.</returns>
    Task<InventoryTransactionDto?> GetTransactionByReferenceNumberAsync(string referenceNumber);

    /// <summary>Retrieves all inventory transactions for a specific product.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <returns>A collection of matching <see cref="InventoryTransactionDto"/> records.</returns>
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductIdAsync(Guid productId);

    /// <summary>Retrieves all inventory transactions for a specific warehouse.</summary>
    /// <param name="warehouseId">The unique identifier of the warehouse.</param>
    /// <returns>A collection of matching <see cref="InventoryTransactionDto"/> records.</returns>
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseIdAsync(Guid warehouseId);

    /// <summary>Retrieves all inventory transactions of a given transaction type.</summary>
    /// <param name="transactionType">The type of transactions to retrieve.</param>
    /// <returns>A collection of matching <see cref="InventoryTransactionDto"/> records.</returns>
    Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByTypeAsync(TransactionType transactionType);

    /// <summary>Retrieves all inventory transactions.</summary>
    /// <returns>A collection of all <see cref="InventoryTransactionDto"/> records.</returns>
    Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync();

    /// <summary>Retrieves a paginated list of all inventory transactions.</summary>
    /// <param name="pageNumber">The one-based page number to return.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PaginatedResult{InventoryTransactionDto}"/> for the requested page.</returns>
    Task<PaginatedResult<InventoryTransactionDto>> GetAllTransactionsPaginatedAsync(int pageNumber, int pageSize);

    /// <summary>Queries inventory transactions using a specification that encapsulates filtering, sorting and pagination.</summary>
    /// <param name="spec">The specification to apply.</param>
    /// <returns>A <see cref="PaginatedResult{InventoryTransactionDto}"/> matching the specification.</returns>
    Task<PaginatedResult<InventoryTransactionDto>> QueryTransactionsAsync(ISpecification<InventoryTransaction> spec);

    /// <summary>Creates a new inventory transaction and updates the affected product's stock level.</summary>
    /// <param name="dto">The data used to create the transaction.</param>
    /// <returns>The newly created <see cref="InventoryTransactionDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the referenced product does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the transaction would result in negative stock.</exception>
    Task<InventoryTransactionDto> CreateTransactionAsync(CreateUpdateInventoryTransactionDto dto);

    /// <summary>Updates an existing inventory transaction and adjusts the product's stock level accordingly.</summary>
    /// <param name="id">The unique identifier of the transaction to update.</param>
    /// <param name="dto">The updated transaction data.</param>
    /// <returns>The updated <see cref="InventoryTransactionDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the transaction or its product is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the update would result in negative stock.</exception>
    Task<InventoryTransactionDto> UpdateTransactionAsync(Guid id, CreateUpdateInventoryTransactionDto dto);

    /// <summary>Deletes an inventory transaction and reverses its effect on the product's stock level.</summary>
    /// <param name="id">The unique identifier of the transaction to delete.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no transaction with the given identifier is found.</exception>
    Task DeleteTransactionAsync(Guid id);
}
