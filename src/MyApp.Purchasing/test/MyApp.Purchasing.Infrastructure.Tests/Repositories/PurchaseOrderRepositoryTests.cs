using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Purchasing.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Purchasing.Tests.Repositories;

/// <summary>Integration tests for <see cref="PurchaseOrderRepository"/> using an in-memory database.</summary>
public class PurchaseOrderRepositoryTests
{
    private readonly PurchasingDbContext _context;
    private readonly PurchaseOrderRepository _repository;

    /// <summary>Initializes a new instance of the <see cref="PurchaseOrderRepositoryTests"/> class, creating a fresh in-memory context and seeding test data.</summary>
    public PurchaseOrderRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new PurchaseOrderRepository(_context);
        SeedTestData();
    }

    /// <summary>Seeds the in-memory database with two suppliers and two purchase orders for use across tests.</summary>
    private void SeedTestData()
    {
        // Clear existing data
        _context.PurchaseOrders.RemoveRange(_context.PurchaseOrders);
        _context.PurchaseOrderLines.RemoveRange(_context.PurchaseOrderLines);
        _context.Suppliers.RemoveRange(_context.Suppliers);
        _context.SaveChanges();

        // Create suppliers
        var supplier1 = new Supplier(Guid.NewGuid())
        {
            Name = "Supplier 1",
            Email = "supplier1@example.com"
        };
        var supplier2 = new Supplier(Guid.NewGuid())
        {
            Name = "Supplier 2",
            Email = "supplier2@example.com"
        };
        _context.Suppliers.AddRange(supplier1, supplier2);
        _context.SaveChanges();

        // Create purchase orders
        var order1 = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = "PO-001",
            SupplierId = supplier1.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = 500.00m
        };
        var order2 = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = "PO-002",
            SupplierId = supplier2.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Approved,
            TotalAmount = 750.00m
        };
        _context.PurchaseOrders.AddRange(order1, order2);
        _context.SaveChanges();
    }

    /// <summary>Creates and persists a <see cref="PurchaseOrder"/> with the given parameters for use in a test.</summary>
    /// <param name="supplierId">The ID of the supplier for the purchase order.</param>
    /// <param name="orderNumber">The order number to assign.</param>
    /// <param name="status">The initial status of the purchase order.</param>
    /// <returns>The persisted <see cref="PurchaseOrder"/> entity.</returns>
    private PurchaseOrder CreateTestPurchaseOrder(Guid supplierId, string orderNumber = "PO-TEST", PurchaseOrderStatus status = PurchaseOrderStatus.Draft)
    {
        var order = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            SupplierId = supplierId,
            OrderDate = DateTime.UtcNow,
            Status = status,
            TotalAmount = 300.00m
        };
        _context.PurchaseOrders.Add(order);
        _context.SaveChanges();
        return order;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetByIdAsync"/> returns the purchase order when a valid ID is provided.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsPurchaseOrder()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var order = CreateTestPurchaseOrder(supplier.Id, "PO-GETBYID");

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.OrderNumber.Should().Be("PO-GETBYID");
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetByIdAsync"/> returns null when the ID does not exist.</summary>
    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetWithLinesAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetWithLinesAsync"/> returns the purchase order with its lines and supplier navigation properties loaded.</summary>
    [Fact]
    public async Task GetWithLinesAsync_WithValidId_ReturnsPurchaseOrderWithIncludes()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var order = CreateTestPurchaseOrder(supplier.Id, "PO-WITH-LINES");

        // Act
        var result = await _repository.GetWithLinesAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.Lines.Should().NotBeNull();
        result.Supplier.Should().NotBeNull();
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetWithLinesAsync"/> returns null when the ID does not exist.</summary>
    [Fact]
    public async Task GetWithLinesAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetWithLinesAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetAllAsync"/> returns all purchase orders in the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllPurchaseOrders()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "PO-GETALL-001");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetAllPaginatedAsync"/> returns the correct page of purchase orders with accurate pagination metadata.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "PO-PAGE-001");
        CreateTestPurchaseOrder(supplier.Id, "PO-PAGE-002");
        CreateTestPurchaseOrder(supplier.Id, "PO-PAGE-003");
        var pageNumber = 1;
        var pageSize = 2;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessThanOrEqualTo(pageSize);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(5);
    }

    #endregion

    #region GetBySuppliersIdAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetBySuppliersIdAsync"/> returns all orders belonging to the specified supplier.</summary>
    [Fact]
    public async Task GetBySuppliersIdAsync_WithExistingOrders_ReturnsAllOrdersForSupplier()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "PO-SUPPLIER-001");
        CreateTestPurchaseOrder(supplier.Id, "PO-SUPPLIER-002");

        // Act
        var result = await _repository.GetBySuppliersIdAsync(supplier.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least 1 seeded + 2 new
        result.All(o => o.SupplierId == supplier.Id).Should().BeTrue();
        result.All(o => o.Lines != null).Should().BeTrue();
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetBySuppliersIdAsync"/> returns an empty collection when no orders exist for the supplier.</summary>
    [Fact]
    public async Task GetBySuppliersIdAsync_WithNoOrders_ReturnsEmptyList()
    {
        // Arrange
        var supplierId = Guid.NewGuid();

        // Act
        var result = await _repository.GetBySuppliersIdAsync(supplierId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByStatusAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetByStatusAsync"/> returns all orders in the specified status.</summary>
    [Fact]
    public async Task GetByStatusAsync_WithExistingOrders_ReturnsOrdersWithStatus()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "PO-STATUS-001", PurchaseOrderStatus.Approved);
        CreateTestPurchaseOrder(supplier.Id, "PO-STATUS-002", PurchaseOrderStatus.Draft);

        // Act
        var result = await _repository.GetByStatusAsync(PurchaseOrderStatus.Approved);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2); // At least 1 seeded + 1 new
        result.All(o => o.Status == PurchaseOrderStatus.Approved).Should().BeTrue();
        result.All(o => o.Lines != null).Should().BeTrue();
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.GetByStatusAsync"/> returns an empty collection when no orders exist in the specified status.</summary>
    [Fact]
    public async Task GetByStatusAsync_WithNoOrdersOfStatus_ReturnsEmptyList()
    {
        // Arrange
        // Clear all orders
        _context.PurchaseOrders.RemoveRange(_context.PurchaseOrders);
        _context.SaveChanges();

        // Create only Draft orders
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "PO-DRAFT", PurchaseOrderStatus.Draft);

        // Act
        var result = await _repository.GetByStatusAsync(PurchaseOrderStatus.Approved);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.AddAsync"/> persists a new purchase order to the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidPurchaseOrder_CreatesPurchaseOrder()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var order = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = "PO-NEW",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = 400.00m
        };

        // Act
        var result = await _repository.AddAsync(order);
        var savedOrder = await _context.PurchaseOrders.FindAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.OrderNumber.Should().Be("PO-NEW");
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.UpdateAsync"/> persists changes to an existing purchase order.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingPurchaseOrder_UpdatesPurchaseOrderData()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var order = CreateTestPurchaseOrder(supplier.Id, "PO-UPDATE");
        order.Status = PurchaseOrderStatus.Approved;
        order.TotalAmount = 500.00m;

        // Act
        var result = await _repository.UpdateAsync(order);
        var updatedOrder = await _context.PurchaseOrders.FindAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(PurchaseOrderStatus.Approved);
        updatedOrder.TotalAmount.Should().Be(500.00m);
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.DeleteAsync"/> removes the purchase order from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidPurchaseOrder_DeletesPurchaseOrder()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var order = CreateTestPurchaseOrder(supplier.Id, "PO-DELETE");

        // Act
        await _repository.DeleteAsync(order);
        await _context.SaveChangesAsync();
        var deletedOrder = await _context.PurchaseOrders.FindAsync(order.Id);

        // Assert
        deletedOrder.Should().BeNull();
    }

    #endregion

    #region QueryAsync Tests

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> filters purchase orders by the free-text search term.</summary>
    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "SEARCH-PO-001");
        CreateTestPurchaseOrder(supplier.Id, "SEARCH-PO-002");
        CreateTestPurchaseOrder(supplier.Id, "OTHER-PO");
        var querySpec = new QuerySpec { SearchTerm = "SEARCH-PO" };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber.Contains("SEARCH-PO", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> filters purchase orders by the order number filter.</summary>
    [Fact]
    public async Task QueryAsync_WithOrderNumberFilter_ShouldFilterResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "FILTER-PO-001");
        CreateTestPurchaseOrder(supplier.Id, "FILTER-PO-002");
        CreateTestPurchaseOrder(supplier.Id, "OTHER-PO");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "OrderNumber", "FILTER-PO" } };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber.Contains("FILTER-PO", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> filters purchase orders by supplier ID.</summary>
    [Fact]
    public async Task QueryAsync_WithSupplierIdFilter_ShouldFilterResults()
    {
        // Arrange
        var supplier1 = _context.Suppliers.First();
        var supplier2 = _context.Suppliers.Skip(1).First();
        CreateTestPurchaseOrder(supplier1.Id, "SUPPLIER1-PO");
        CreateTestPurchaseOrder(supplier2.Id, "SUPPLIER2-PO");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SupplierId", supplier1.Id.ToString() } };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.SupplierId == supplier1.Id);
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> filters purchase orders by status.</summary>
    [Fact]
    public async Task QueryAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "STATUS-001", PurchaseOrderStatus.Approved);
        CreateTestPurchaseOrder(supplier.Id, "STATUS-002", PurchaseOrderStatus.Draft);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Status", PurchaseOrderStatus.Approved.ToString() } };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.Status == PurchaseOrderStatus.Approved);
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> filters purchase orders within the specified minimum and maximum total amount range.</summary>
    [Fact]
    public async Task QueryAsync_WithTotalAmountRangeFilter_ShouldFilterResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        var order1 = CreateTestPurchaseOrder(supplier.Id, "TOTAL-001");
        order1.TotalAmount = 100.00m;
        _context.SaveChanges();
        var order2 = CreateTestPurchaseOrder(supplier.Id, "TOTAL-002");
        order2.TotalAmount = 250.00m;
        _context.SaveChanges();
        var order3 = CreateTestPurchaseOrder(supplier.Id, "TOTAL-003");
        order3.TotalAmount = 500.00m;
        _context.SaveChanges();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string>
        {
            { "TotalAmountMin", "150" },
            { "TotalAmountMax", "400" }
        };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.TotalAmount >= 150m && o.TotalAmount <= 400m);
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> returns the correct page of results when pagination is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "PAGE-QUERY-001");
        CreateTestPurchaseOrder(supplier.Id, "PAGE-QUERY-002");
        CreateTestPurchaseOrder(supplier.Id, "PAGE-QUERY-003");
        CreateTestPurchaseOrder(supplier.Id, "PAGE-QUERY-004");
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(6);
    }

    /// <summary>Verifies that <see cref="PurchaseOrderRepository.QueryAsync"/> returns purchase orders sorted in ascending order when a sort field is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var supplier = _context.Suppliers.First();
        CreateTestPurchaseOrder(supplier.Id, "ZEBRA-PO");
        CreateTestPurchaseOrder(supplier.Id, "ALPHA-PO");
        CreateTestPurchaseOrder(supplier.Id, "BETA-PO");
        var querySpec = new QuerySpec { SortBy = "OrderNumber", SortDesc = false };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var orderNumbers = result.Items.Select(o => o.OrderNumber).ToList();
        var sortedOrderNumbers = orderNumbers.OrderBy(n => n).ToList();
        orderNumbers.Should().BeEquivalentTo(sortedOrderNumbers);
    }

    #endregion
}

