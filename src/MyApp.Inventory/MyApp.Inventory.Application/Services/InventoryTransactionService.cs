using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Application.Services;

/// <summary>Provides inventory transaction management operations for the Inventory service.</summary>
public class InventoryTransactionService : AppServiceBase, IInventoryTransactionService
{
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="InventoryTransactionService"/>.</summary>
    /// Initializes a new instance of the InventoryTransactionService class.
    /// <param name="transactionRepository">The transaction Repository.</param>
    /// <param name="productRepository">The product Repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public InventoryTransactionService(
        IInventoryTransactionRepository transactionRepository,
        IProductRepository productRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<InventoryTransactionService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Inventory)
    {
        _transactionRepository = transactionRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    /// <summary>Retrieves an inventory transaction by its unique identifier.</summary>
    /// Gets the transaction by id asynchronously.
    /// <param name="id">The id.</param>
    /// <returns>The matching <see cref="InventoryTransactionDto"/>, or <c>null</c> if not found.</returns>
    public async Task<InventoryTransactionDto?> GetTransactionByIdAsync(Guid id)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        return transaction == null ? null : _mapper.Map<InventoryTransactionDto>(transaction);
    }

    /// <summary>Retrieves an inventory transaction by its external reference number.</summary>
    /// Gets the transaction by reference number asynchronously.
    /// <param name="referenceNumber">The reference Number.</param>
    /// <returns>The matching <see cref="InventoryTransactionDto"/>, or <c>null</c> if not found.</returns>
    public async Task<InventoryTransactionDto?> GetTransactionByReferenceNumberAsync(string referenceNumber)
    {
        var transaction = await _transactionRepository.GetByReferenceNumberAsync(referenceNumber);
        return transaction == null ? null : _mapper.Map<InventoryTransactionDto>(transaction);
    }

    /// <summary>Retrieves all inventory transactions for a specific product.</summary>
    /// Gets the transactions by product id asynchronously.
    /// <param name="productId">The product Id.</param>
    /// <returns>A collection of matching <see cref="InventoryTransactionDto"/> records.</returns>
    public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductIdAsync(Guid productId)
    {
        var transactions = await _transactionRepository.GetByProductIdAsync(productId);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    /// <summary>Retrieves all inventory transactions for a specific warehouse.</summary>
    /// Gets the transactions by warehouse id asynchronously.
    /// <param name="warehouseId">The warehouse Id.</param>
    /// <returns>A collection of matching <see cref="InventoryTransactionDto"/> records.</returns>
    public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseIdAsync(Guid warehouseId)
    {
        var transactions = await _transactionRepository.GetByWarehouseIdAsync(warehouseId);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    /// <summary>Retrieves all inventory transactions of a given transaction type.</summary>
    /// Gets the transactions by type asynchronously.
    /// <param name="transactionType">The transaction Type.</param>
    /// <returns>A collection of matching <see cref="InventoryTransactionDto"/> records.</returns>
    public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByTypeAsync(TransactionType transactionType)
    {
        var transactions = await _transactionRepository.GetByTransactionTypeAsync(transactionType);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    /// <summary>Retrieves all inventory transactions.</summary>
    /// Gets all transactions asynchronously.
    public async Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync()
    {
        var transactions = await _transactionRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    /// <summary>Retrieves a paginated list of all inventory transactions.</summary>
    /// Gets all transactions paginated asynchronously.
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <returns>A <see cref="PaginatedResult{InventoryTransactionDto}"/> for the requested page.</returns>
    public async Task<PaginatedResult<InventoryTransactionDto>> GetAllTransactionsPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedTransactions = await _transactionRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var transactionDtos = _mapper.Map<IEnumerable<InventoryTransactionDto>>(paginatedTransactions.Items);
        return new PaginatedResult<InventoryTransactionDto>(transactionDtos, paginatedTransactions.PageNumber, paginatedTransactions.PageSize, paginatedTransactions.TotalCount);
    }

    /// <summary>Creates a new inventory transaction and updates the affected product's stock level.</summary>
    /// Creates a transaction asynchronously.
    /// <param name="dto">The dto.</param>
    /// <returns>The newly created <see cref="InventoryTransactionDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the referenced product does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the transaction would result in negative stock.</exception>
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
        await SaveChangesAsync();

        // Load related data for response
        createdTransaction = await _transactionRepository.GetByIdAsync(createdTransaction.Id);

        return _mapper.Map<InventoryTransactionDto>(createdTransaction);
    }

    /// <summary>Updates an existing inventory transaction and adjusts the product's stock level accordingly.</summary>
    /// Updates the transaction asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <returns>The updated <see cref="InventoryTransactionDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the transaction or its product is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the update would result in negative stock.</exception>
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
        await SaveChangesAsync();

        return _mapper.Map<InventoryTransactionDto>(updatedTransaction);
    }

    /// <summary>Deletes an inventory transaction and reverses its effect on the product's stock level.</summary>
    /// Deletes the transaction asynchronously.
    /// <param name="id">The id.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no transaction with the given identifier is found.</exception>
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
        await SaveChangesAsync();
    }

    /// <summary>Queries inventory transactions using a specification that encapsulates filtering, sorting and pagination.</summary>
    /// Query transactions asynchronously.
    /// <param name="spec">The spec.</param>
    /// <returns>A <see cref="PaginatedResult{InventoryTransactionDto}"/> matching the specification.</returns>
    public async Task<PaginatedResult<InventoryTransactionDto>> QueryTransactionsAsync(ISpecification<InventoryTransaction> spec)
    {
        var result = await _transactionRepository.QueryAsync(spec);
        var dtos = result.Items.Select(t => _mapper.Map<InventoryTransactionDto>(t)).ToList();
        return new PaginatedResult<InventoryTransactionDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
