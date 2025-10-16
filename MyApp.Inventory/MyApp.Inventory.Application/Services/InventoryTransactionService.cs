using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Application.Services;

public class InventoryTransactionService : IInventoryTransactionService
{
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public InventoryTransactionService(
        IInventoryTransactionRepository transactionRepository,
        IProductRepository productRepository,
        IMapper mapper)
    {
        _transactionRepository = transactionRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<InventoryTransactionDto?> GetTransactionByIdAsync(Guid id)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        return transaction == null ? null : _mapper.Map<InventoryTransactionDto>(transaction);
    }

    public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductIdAsync(Guid productId)
    {
        var transactions = await _transactionRepository.GetByProductIdAsync(productId);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseIdAsync(Guid warehouseId)
    {
        var transactions = await _transactionRepository.GetByWarehouseIdAsync(warehouseId);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByTypeAsync(TransactionType transactionType)
    {
        var transactions = await _transactionRepository.GetByTransactionTypeAsync(transactionType);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    public async Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync()
    {
        var transactions = await _transactionRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    public async Task<InventoryTransactionDto> CreateTransactionAsync(CreateUpdateInventoryTransactionDto dto)
    {
        // Verify product exists
        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{dto.ProductId}' not found.");
        }

        var transaction = _mapper.Map<InventoryTransaction>(dto);
        var createdTransaction = await _transactionRepository.AddAsync(transaction);

        // Update product stock based on transaction
        product.QuantityInStock += dto.QuantityChange;

        // Prevent negative stock levels
        if (product.QuantityInStock < 0)
        {
            throw new InvalidOperationException(
                $"Insufficient stock. Product '{product.Name}' has {product.QuantityInStock - dto.QuantityChange} units available.");
        }

        await _productRepository.UpdateAsync(product);

        // Load related data for response
        createdTransaction = await _transactionRepository.GetByIdAsync(createdTransaction.Id);

        return _mapper.Map<InventoryTransactionDto>(createdTransaction);
    }

    public async Task<InventoryTransactionDto> UpdateTransactionAsync(Guid id, CreateUpdateInventoryTransactionDto dto)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
        {
            throw new KeyNotFoundException($"Transaction with ID '{id}' not found.");
        }

        // Get product to reverse old transaction
        var product = await _productRepository.GetByIdAsync(transaction.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{transaction.ProductId}' not found.");
        }

        // Reverse the old transaction
        product.QuantityInStock -= transaction.QuantityChange;

        // Apply new transaction
        product.QuantityInStock += dto.QuantityChange;

        // Prevent negative stock levels
        if (product.QuantityInStock < 0)
        {
            throw new InvalidOperationException(
                $"Insufficient stock. Product '{product.Name}' would have negative quantity.");
        }

        _mapper.Map(dto, transaction);
        var updatedTransaction = await _transactionRepository.UpdateAsync(transaction);
        await _productRepository.UpdateAsync(product);

        return _mapper.Map<InventoryTransactionDto>(updatedTransaction);
    }

    public async Task DeleteTransactionAsync(Guid id)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
        {
            throw new KeyNotFoundException($"Transaction with ID '{id}' not found.");
        }

        // Get product to reverse the transaction
        var product = await _productRepository.GetByIdAsync(transaction.ProductId);
        if (product != null)
        {
            product.QuantityInStock -= transaction.QuantityChange;
            await _productRepository.UpdateAsync(product);
        }

        await _transactionRepository.DeleteAsync(transaction);
    }
}
