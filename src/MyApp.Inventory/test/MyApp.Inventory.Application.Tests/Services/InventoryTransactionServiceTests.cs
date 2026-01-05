using AutoMapper;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Services;

public class InventoryTransactionServiceTests
{
    private readonly Mock<IInventoryTransactionRepository> _mockTransactionRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly InventoryTransactionService _transactionService;

    public InventoryTransactionServiceTests()
    {
        _mockTransactionRepository = new Mock<IInventoryTransactionRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockMapper = new Mock<IMapper>();

        _transactionService = new InventoryTransactionService(
            _mockTransactionRepository.Object,
            _mockProductRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WithExistingId_ReturnsTransactionDto()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = new InventoryTransaction(transactionId);
        var expectedDto = new InventoryTransactionDto(transactionId, default, default, 0, default, default, null, null);

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(transaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(transaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.GetTransactionByIdAsync(transactionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transactionId, result.Id);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync((InventoryTransaction?)null);

        // Act
        var result = await _transactionService.GetTransactionByIdAsync(transactionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTransactionsByProductIdAsync_ReturnsTransactionsForProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { ProductId = productId }
        };
        var dtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, productId, default, 0, default, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.GetByProductIdAsync(productId)).ReturnsAsync(transactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(transactions)).Returns(dtos);

        // Act
        var result = await _transactionService.GetTransactionsByProductIdAsync(productId);

        // Assert
        Assert.Single(result);
        _mockTransactionRepository.Verify(r => r.GetByProductIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByWarehouseIdAsync_ReturnsTransactionsForWarehouse()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { WarehouseId = warehouseId }
        };
        var dtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, default, warehouseId, 0, default, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.GetByWarehouseIdAsync(warehouseId)).ReturnsAsync(transactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(transactions)).Returns(dtos);

        // Act
        var result = await _transactionService.GetTransactionsByWarehouseIdAsync(warehouseId);

        // Assert
        Assert.Single(result);
        _mockTransactionRepository.Verify(r => r.GetByWarehouseIdAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByTypeAsync_ReturnsTransactionsOfType()
    {
        // Arrange
        var transactionType = TransactionType.Inbound;
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { TransactionType = transactionType }
        };
        var dtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, default, default, 0, default, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.GetByTransactionTypeAsync(transactionType)).ReturnsAsync(transactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(transactions)).Returns(dtos);

        // Act
        var result = await _transactionService.GetTransactionsByTypeAsync(transactionType);

        // Assert
        Assert.Single(result);
        _mockTransactionRepository.Verify(r => r.GetByTransactionTypeAsync(transactionType), Times.Once);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_ReturnsAllTransactions()
    {
        // Arrange
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()),
            new InventoryTransaction(Guid.NewGuid())
        };
        var dtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, default, default, 0, default, default, null, null),
            new InventoryTransactionDto(default, default, default, 0, default, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(transactions)).Returns(dtos);

        // Act
        var result = await _transactionService.GetAllTransactionsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _mockTransactionRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithValidData_CreatesTransactionAndUpdatesStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 50, TransactionType.Inbound, default);
        var transaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 50 };
        var createdTransaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 50 };
        var expectedDto = new InventoryTransactionDto(default, default, default, 0, default, default, null, null);

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<InventoryTransaction>(dto)).Returns(transaction);
        _mockTransactionRepository.Setup(r => r.AddAsync(transaction)).ReturnsAsync(createdTransaction);
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(createdTransaction.Id)).ReturnsAsync(createdTransaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(createdTransaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, product.QuantityInStock); // 100 + 50
        _mockProductRepository.Verify(r => r.UpdateAsync(product), Times.Once);
        _mockTransactionRepository.Verify(r => r.AddAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithNonExistentProduct_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 0, TransactionType.Inbound, default);

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.CreateTransactionAsync(dto));

        Assert.Contains("not found", exception.Message);
        _mockTransactionRepository.Verify(r => r.AddAsync(It.IsAny<InventoryTransaction>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { Name = "Test Product", QuantityInStock = 10 };
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), -20, TransactionType.Outbound, default);
        var transaction = new InventoryTransaction(Guid.NewGuid()) { QuantityChange = -20 };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<InventoryTransaction>(dto)).Returns(transaction);
        _mockTransactionRepository.Setup(r => r.AddAsync(transaction)).ReturnsAsync(transaction);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionService.CreateTransactionAsync(dto));

        Assert.Contains("Insufficient stock", exception.Message);
        _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithValidData_UpdatesTransactionAndStock()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var existingTransaction = new InventoryTransaction(transactionId)
        {
            ProductId = productId,
            QuantityChange = 10
        };
        var updateDto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 20, TransactionType.Inbound, default);
        var updatedTransaction = new InventoryTransaction(Guid.NewGuid()) { QuantityChange = 20 };
        var expectedDto = new InventoryTransactionDto(default, default, default, 0, default, default, null, null);

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(existingTransaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map(updateDto, existingTransaction));
        _mockTransactionRepository.Setup(r => r.UpdateAsync(existingTransaction)).ReturnsAsync(updatedTransaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(updatedTransaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.UpdateTransactionAsync(transactionId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(110, product.QuantityInStock); // 100 - 10 (reverse old) + 20 (apply new)
        _mockProductRepository.Verify(r => r.UpdateAsync(product), Times.Once);
        _mockTransactionRepository.Verify(r => r.UpdateAsync(existingTransaction), Times.Once);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithNonExistentTransaction_ThrowsKeyNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var updateDto = new CreateUpdateInventoryTransactionDto(Guid.NewGuid(), Guid.NewGuid(), 0, TransactionType.Inbound, default);

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync((InventoryTransaction?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.UpdateTransactionAsync(transactionId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockTransactionRepository.Verify(r => r.UpdateAsync(It.IsAny<InventoryTransaction>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product(productId) { Name = "Test Product", QuantityInStock = 100 };
        var existingTransaction = new InventoryTransaction(transactionId)
        {
            ProductId = productId,
            QuantityChange = 10
        };
        var updateDto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), -200, TransactionType.Outbound, default);

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(existingTransaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionService.UpdateTransactionAsync(transactionId, updateDto));

        Assert.Contains("Insufficient stock", exception.Message);
        _mockTransactionRepository.Verify(r => r.UpdateAsync(It.IsAny<InventoryTransaction>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithExistingTransaction_DeletesAndReversesStock()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 150 };
        var transaction = new InventoryTransaction(transactionId)
        {
            ProductId = productId,
            QuantityChange = 50
        };

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(transaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act
        await _transactionService.DeleteTransactionAsync(transactionId);

        // Assert
        Assert.Equal(100, product.QuantityInStock); // 150 - 50
        _mockProductRepository.Verify(r => r.UpdateAsync(product), Times.Once);
        _mockTransactionRepository.Verify(r => r.DeleteAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithNonExistentTransaction_ThrowsKeyNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync((InventoryTransaction?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.DeleteTransactionAsync(transactionId));

        Assert.Contains("not found", exception.Message);
        _mockTransactionRepository.Verify(r => r.DeleteAsync(It.IsAny<InventoryTransaction>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithNonExistentProduct_StillDeletesTransaction()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transaction = new InventoryTransaction(transactionId)
        {
            ProductId = productId,
            QuantityChange = 50
        };

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(transaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act
        await _transactionService.DeleteTransactionAsync(transactionId);

        // Assert
        _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        _mockTransactionRepository.Verify(r => r.DeleteAsync(transaction), Times.Once);
    }
}
