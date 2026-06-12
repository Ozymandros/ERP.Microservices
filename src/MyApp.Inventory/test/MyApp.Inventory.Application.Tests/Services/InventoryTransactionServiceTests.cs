using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Services;

public class InventoryTransactionServiceTests
{
    private readonly Mock<IInventoryTransactionRepository> _mockTransactionRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<InventoryTransactionService>> _mockLogger;
    private readonly InventoryTransactionService _transactionService;

    public InventoryTransactionServiceTests()
    {
        _mockTransactionRepository = new Mock<IInventoryTransactionRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<InventoryTransactionService>>();
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        _transactionService = new InventoryTransactionService(
            _mockTransactionRepository.Object,
            _mockProductRepository.Object,
            _mockMapper.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
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

    #region GetTransactionByReferenceNumberAsync Tests

    [Fact]
    public async Task GetTransactionByReferenceNumberAsync_WithExistingReference_ReturnsTransactionDto()
    {
        // Arrange
        var referenceNumber = "REF-001";
        var transaction = new InventoryTransaction(Guid.NewGuid()) { ReferenceNumber = referenceNumber };
        var expectedDto = new InventoryTransactionDto(
            transaction.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            TransactionType.Inbound,
            DateTime.UtcNow,
            null,
            null,
            referenceNumber);

        _mockTransactionRepository.Setup(r => r.GetByReferenceNumberAsync(referenceNumber)).ReturnsAsync(transaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(transaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.GetTransactionByReferenceNumberAsync(referenceNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(referenceNumber, result.ReferenceNumber);
        _mockTransactionRepository.Verify(r => r.GetByReferenceNumberAsync(referenceNumber), Times.Once);
    }

    [Fact]
    public async Task GetTransactionByReferenceNumberAsync_WithNonExistentReference_ReturnsNull()
    {
        // Arrange
        var referenceNumber = "NONEXISTENT";
        _mockTransactionRepository.Setup(r => r.GetByReferenceNumberAsync(referenceNumber)).ReturnsAsync((InventoryTransaction?)null);

        // Act
        var result = await _transactionService.GetTransactionByReferenceNumberAsync(referenceNumber);

        // Assert
        Assert.Null(result);
        _mockTransactionRepository.Verify(r => r.GetByReferenceNumberAsync(referenceNumber), Times.Once);
    }

    #endregion

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

    #region GetAllTransactionsPaginatedAsync Tests

    [Fact]
    public async Task GetAllTransactionsPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { ProductId = Guid.NewGuid(), QuantityChange = 10 },
            new InventoryTransaction(Guid.NewGuid()) { ProductId = Guid.NewGuid(), QuantityChange = 20 },
            new InventoryTransaction(Guid.NewGuid()) { ProductId = Guid.NewGuid(), QuantityChange = 30 }
        };

        var paginatedTransactions = new PaginatedResult<InventoryTransaction>(
            transactions.Take(2),
            1,
            2,
            3
        );

        var transactionDtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, default, default, 10, default, default, null, null),
            new InventoryTransactionDto(default, default, default, 20, default, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.GetAllPaginatedAsync(1, 2)).ReturnsAsync(paginatedTransactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(It.IsAny<IEnumerable<InventoryTransaction>>())).Returns(transactionDtos);

        // Act
        var result = await _transactionService.GetAllTransactionsPaginatedAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        _mockTransactionRepository.Verify(r => r.GetAllPaginatedAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task GetAllTransactionsPaginatedAsync_WithEmptyResult_ReturnsEmptyPaginatedResult()
    {
        // Arrange
        var paginatedTransactions = new PaginatedResult<InventoryTransaction>(
            Enumerable.Empty<InventoryTransaction>(),
            1,
            20,
            0
        );

        _mockTransactionRepository.Setup(r => r.GetAllPaginatedAsync(1, 20)).ReturnsAsync(paginatedTransactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(It.IsAny<IEnumerable<InventoryTransaction>>())).Returns(Enumerable.Empty<InventoryTransactionDto>());

        // Act
        var result = await _transactionService.GetAllTransactionsPaginatedAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region QueryTransactionsAsync Tests

    [Fact]
    public async Task QueryTransactionsAsync_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { ReferenceNumber = "REF-SEARCH-001" }
        };

        var querySpec = new QuerySpec { SearchTerm = "SEARCH" };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<InventoryTransaction>(transactions, 1, 20, 1);

        var transactionDtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, default, default, 0, default, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(It.IsAny<InventoryTransaction>())).Returns((InventoryTransaction t) =>
            new InventoryTransactionDto(t.Id, t.ProductId, t.WarehouseId, t.QuantityChange, t.TransactionType, t.TransactionDate, null, null));

        // Act
        var result = await _transactionService.QueryTransactionsAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        _mockTransactionRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    [Fact]
    public async Task QueryTransactionsAsync_WithTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { TransactionType = TransactionType.Adjustment }
        };

        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "TransactionType", TransactionType.Adjustment.ToString() } };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<InventoryTransaction>(transactions, 1, 20, 1);

        _mockTransactionRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(It.IsAny<InventoryTransaction>())).Returns((InventoryTransaction t) =>
            new InventoryTransactionDto(t.Id, t.ProductId, t.WarehouseId, t.QuantityChange, t.TransactionType, t.TransactionDate, null, null));

        // Act
        var result = await _transactionService.QueryTransactionsAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _mockTransactionRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    #endregion

    #region Transaction Type Tests

    [Fact]
    public async Task CreateTransactionAsync_WithAdjustmentType_CreatesTransaction()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 25, TransactionType.Adjustment, DateTime.UtcNow);
        var transaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 25, TransactionType = TransactionType.Adjustment };
        var createdTransaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 25, TransactionType = TransactionType.Adjustment };
        var expectedDto = new InventoryTransactionDto(default, default, default, 25, TransactionType.Adjustment, default, null, null);

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<InventoryTransaction>(dto)).Returns(transaction);
        _mockTransactionRepository.Setup(r => r.AddAsync(transaction)).ReturnsAsync(createdTransaction);
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(createdTransaction.Id)).ReturnsAsync(createdTransaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(createdTransaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TransactionType.Should().Be(TransactionType.Adjustment);
        Assert.Equal(125, product.QuantityInStock); // 100 + 25
    }

    [Fact]
    public async Task GetTransactionsByTypeAsync_WithAdjustmentType_ReturnsAdjustmentTransactions()
    {
        // Arrange
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { TransactionType = TransactionType.Adjustment }
        };
        var dtos = new List<InventoryTransactionDto>
        {
            new InventoryTransactionDto(default, default, default, 0, TransactionType.Adjustment, default, null, null)
        };

        _mockTransactionRepository.Setup(r => r.GetByTransactionTypeAsync(TransactionType.Adjustment)).ReturnsAsync(transactions);
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(transactions)).Returns(dtos);

        // Act
        var result = await _transactionService.GetTransactionsByTypeAsync(TransactionType.Adjustment);

        // Assert
        result.Should().HaveCount(1);
        result.First().TransactionType.Should().Be(TransactionType.Adjustment);
        _mockTransactionRepository.Verify(r => r.GetByTransactionTypeAsync(TransactionType.Adjustment), Times.Once);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task CreateTransactionAsync_WithZeroQuantityChange_CreatesTransaction()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 0, TransactionType.Adjustment, DateTime.UtcNow);
        var transaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 0 };
        var createdTransaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 0 };
        var expectedDto = new InventoryTransactionDto(default, default, default, 0, default, default, null, null);

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<InventoryTransaction>(dto)).Returns(transaction);
        _mockTransactionRepository.Setup(r => r.AddAsync(transaction)).ReturnsAsync(createdTransaction);
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(createdTransaction.Id)).ReturnsAsync(createdTransaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(createdTransaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        result.Should().NotBeNull();
        Assert.Equal(100, product.QuantityInStock); // Unchanged
    }

    [Fact]
    public async Task CreateTransactionAsync_WithLargeQuantityChange_CreatesTransaction()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var largeQuantity = 1000000; // Large but safe quantity
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), largeQuantity, TransactionType.Inbound, DateTime.UtcNow);
        var transaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = largeQuantity };
        var createdTransaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = largeQuantity };
        var expectedDto = new InventoryTransactionDto(default, default, default, largeQuantity, default, default, null, null);

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<InventoryTransaction>(dto)).Returns(transaction);
        _mockTransactionRepository.Setup(r => r.AddAsync(transaction)).ReturnsAsync(createdTransaction);
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(createdTransaction.Id)).ReturnsAsync(createdTransaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(createdTransaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        result.Should().NotBeNull();
        Assert.Equal(100 + largeQuantity, product.QuantityInStock);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithZeroQuantityChange_UpdatesSuccessfully()
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
        var updateDto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 0, TransactionType.Adjustment, DateTime.UtcNow);
        var updatedTransaction = new InventoryTransaction(transactionId) { QuantityChange = 0 };
        var expectedDto = new InventoryTransactionDto(default, default, default, 0, default, default, null, null);

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(existingTransaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map(updateDto, existingTransaction));
        _mockTransactionRepository.Setup(r => r.UpdateAsync(existingTransaction)).ReturnsAsync(updatedTransaction);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(updatedTransaction)).Returns(expectedDto);

        // Act
        var result = await _transactionService.UpdateTransactionAsync(transactionId, updateDto);

        // Assert
        result.Should().NotBeNull();
        Assert.Equal(90, product.QuantityInStock); // 100 - 10 (reverse old) + 0 (apply new)
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenProductUpdateFails_DoesNotCreateTransaction()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var dto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 50, TransactionType.Inbound, DateTime.UtcNow);
        var transaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 50 };
        var createdTransaction = new InventoryTransaction(Guid.NewGuid()) { ProductId = productId, QuantityChange = 50 };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<InventoryTransaction>(dto)).Returns(transaction);
        _mockTransactionRepository.Setup(r => r.AddAsync(transaction)).ReturnsAsync(createdTransaction);
        _mockTransactionRepository.Setup(r => r.GetByIdAsync(createdTransaction.Id)).ReturnsAsync(createdTransaction);
        _mockProductRepository.Setup(r => r.UpdateAsync(product)).ThrowsAsync(new Exception("Update failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _transactionService.CreateTransactionAsync(dto));
    }

    [Fact]
    public async Task GetAllTransactionsAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockTransactionRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<InventoryTransaction>());
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(It.IsAny<IEnumerable<InventoryTransaction>>())).Returns(Enumerable.Empty<InventoryTransactionDto>());

        // Act
        var result = await _transactionService.GetAllTransactionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsByProductIdAsync_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockTransactionRepository.Setup(r => r.GetByProductIdAsync(productId)).ReturnsAsync(Enumerable.Empty<InventoryTransaction>());
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(It.IsAny<IEnumerable<InventoryTransaction>>())).Returns(Enumerable.Empty<InventoryTransactionDto>());

        // Act
        var result = await _transactionService.GetTransactionsByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsByWarehouseIdAsync_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        _mockTransactionRepository.Setup(r => r.GetByWarehouseIdAsync(warehouseId)).ReturnsAsync(Enumerable.Empty<InventoryTransaction>());
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(It.IsAny<IEnumerable<InventoryTransaction>>())).Returns(Enumerable.Empty<InventoryTransactionDto>());

        // Act
        var result = await _transactionService.GetTransactionsByWarehouseIdAsync(warehouseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsByTypeAsync_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        _mockTransactionRepository.Setup(r => r.GetByTransactionTypeAsync(TransactionType.Adjustment)).ReturnsAsync(Enumerable.Empty<InventoryTransaction>());
        _mockMapper.Setup(m => m.Map<IEnumerable<InventoryTransactionDto>>(It.IsAny<IEnumerable<InventoryTransaction>>())).Returns(Enumerable.Empty<InventoryTransactionDto>());

        // Act
        var result = await _transactionService.GetTransactionsByTypeAsync(TransactionType.Adjustment);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTransactionAsync_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingTransaction = new InventoryTransaction(transactionId)
        {
            ProductId = productId,
            QuantityChange = 10
        };
        var updateDto = new CreateUpdateInventoryTransactionDto(productId, Guid.NewGuid(), 20, TransactionType.Inbound, DateTime.UtcNow);

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(existingTransaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.UpdateTransactionAsync(transactionId, updateDto));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithZeroQuantityChange_DeletesSuccessfully()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product(productId) { QuantityInStock = 100 };
        var transaction = new InventoryTransaction(transactionId)
        {
            ProductId = productId,
            QuantityChange = 0
        };

        _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync(transaction);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act
        await _transactionService.DeleteTransactionAsync(transactionId);

        // Assert
        Assert.Equal(100, product.QuantityInStock); // Unchanged
        _mockTransactionRepository.Verify(r => r.DeleteAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task QueryTransactionsAsync_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { QuantityChange = 10 },
            new InventoryTransaction(Guid.NewGuid()) { QuantityChange = 20 }
        };

        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<InventoryTransaction>(transactions, 1, 2, 10);

        _mockTransactionRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<InventoryTransactionDto>(It.IsAny<InventoryTransaction>())).Returns((InventoryTransaction t) =>
            new InventoryTransactionDto(t.Id, t.ProductId, t.WarehouseId, t.QuantityChange, t.TransactionType, t.TransactionDate, null, null));

        // Act
        var result = await _transactionService.QueryTransactionsAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(10);
    }

    #endregion
}
