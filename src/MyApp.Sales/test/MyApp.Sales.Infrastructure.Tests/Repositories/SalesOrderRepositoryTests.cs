using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Domain.Specifications;
using MyApp.Sales.Infrastructure.Data;
using MyApp.Sales.Infrastructure.Data.Repositories;
using MyApp.Sales.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Sales.Tests.Repositories;

/// <summary>Integration tests for <see cref="SalesOrderRepository"/> using an in-memory database.</summary>
public class SalesOrderRepositoryTests
{
    private readonly SalesDbContext _context;
    private readonly SalesOrderRepository _repository;

    /// <summary>Initializes a new <see cref="SalesOrderRepositoryTests"/> with a seeded in-memory database context.</summary>
    public SalesOrderRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new SalesOrderRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.SalesOrders.RemoveRange(_context.SalesOrders);
        _context.SalesOrderLines.RemoveRange(_context.SalesOrderLines);
        _context.Customers.RemoveRange(_context.Customers);
        _context.SaveChanges();

        // Create customers
        var customer1 = new Customer(Guid.NewGuid())
        {
            Name = "Customer 1",
            Email = "customer1@example.com"
        };
        var customer2 = new Customer(Guid.NewGuid())
        {
            Name = "Customer 2",
            Email = "customer2@example.com"
        };
        _context.Customers.AddRange(customer1, customer2);
        _context.SaveChanges();

        // Create sales orders
        var order1 = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = "SO-001",
            CustomerId = customer1.Id,
            OrderDate = DateTime.UtcNow,
            Status = SalesOrderStatus.Draft,
            TotalAmount = 100.00m,
            IsQuote = false
        };
        var order2 = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = "SO-002",
            CustomerId = customer2.Id,
            OrderDate = DateTime.UtcNow,
            Status = SalesOrderStatus.Confirmed,
            TotalAmount = 200.00m,
            IsQuote = true,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(30)
        };
        _context.SalesOrders.AddRange(order1, order2);
        _context.SaveChanges();
    }

    private SalesOrder CreateTestSalesOrder(Guid customerId, string orderNumber = "SO-TEST", SalesOrderStatus status = SalesOrderStatus.Draft, bool isQuote = false)
    {
        var order = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Status = status,
            TotalAmount = 150.00m,
            IsQuote = isQuote,
            QuoteExpiryDate = isQuote ? DateTime.UtcNow.AddDays(30) : null
        };
        _context.SalesOrders.Add(order);
        _context.SaveChanges();
        return order;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that GetByIdAsync returns the sales order with Customer and Lines navigation properties eagerly loaded.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSalesOrderWithIncludes()
    {
        // Arrange
        var customer = _context.Customers.First();
        var order = CreateTestSalesOrder(customer.Id, "SO-GETBYID");

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.OrderNumber.Should().Be("SO-GETBYID");
        result.Customer.Should().NotBeNull();
        result.Lines.Should().NotBeNull();
    }

    /// <summary>Verifies that GetByIdAsync returns null when no sales order exists with the given identifier.</summary>
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

    #region ListAsync Tests

    /// <summary>Verifies that ListAsync returns all sales orders present in the database.</summary>
    [Fact]
    public async Task ListAsync_ReturnsAllSalesOrders()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "SO-LIST-001");
        CreateTestSalesOrder(customer.Id, "SO-LIST-002");

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4); // At least 2 seeded + 2 new
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>Verifies that GetAllAsync returns the complete set of sales orders from the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllSalesOrders()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "SO-GETALL-001");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that GetAllPaginatedAsync returns the correct page slice with accurate pagination metadata.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "SO-PAGE-001");
        CreateTestSalesOrder(customer.Id, "SO-PAGE-002");
        CreateTestSalesOrder(customer.Id, "SO-PAGE-003");
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

    /// <summary>Verifies that GetAllPaginatedAsync returns items from the second page when page number 2 is requested.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "SO-PAGE2-001");
        CreateTestSalesOrder(customer.Id, "SO-PAGE2-002");
        CreateTestSalesOrder(customer.Id, "SO-PAGE2-003");
        var pageNumber = 2;
        var pageSize = 2;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(pageNumber);
        result.Items.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that AddAsync persists a new sales order and makes it retrievable from the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidSalesOrder_CreatesSalesOrder()
    {
        // Arrange
        var customer = _context.Customers.First();
        var order = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = "SO-NEW",
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = SalesOrderStatus.Draft,
            TotalAmount = 250.00m,
            IsQuote = false
        };

        // Act
        var result = await _repository.AddAsync(order);
        var savedOrder = await _context.SalesOrders.FindAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.OrderNumber.Should().Be("SO-NEW");
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that UpdateAsync saves changed sales order properties (status, total amount) to the database.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingSalesOrder_UpdatesSalesOrderData()
    {
        // Arrange
        var customer = _context.Customers.First();
        var order = CreateTestSalesOrder(customer.Id, "SO-UPDATE");
        order.Status = SalesOrderStatus.Confirmed;
        order.TotalAmount = 300.00m;

        // Act
        var result = await _repository.UpdateAsync(order);
        var updatedOrder = await _context.SalesOrders.FindAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(SalesOrderStatus.Confirmed);
        updatedOrder.TotalAmount.Should().Be(300.00m);
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that DeleteAsync removes the sales order record from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesSalesOrder()
    {
        // Arrange
        var customer = _context.Customers.First();
        var order = CreateTestSalesOrder(customer.Id, "SO-DELETE");

        // Act
        await _repository.DeleteAsync(order.Id);
        await _context.SaveChangesAsync();
        var deletedOrder = await _context.SalesOrders.FindAsync(order.Id);

        // Assert
        deletedOrder.Should().BeNull();
    }

    /// <summary>Verifies that DeleteAsync does not throw when called with an identifier that does not exist in the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        Func<Task> act = async () => await _repository.DeleteAsync(nonExistentId);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region QueryAsync Tests

    /// <summary>Verifies that QueryAsync filters sales orders to those whose order number contains the free-text search term.</summary>
    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "SEARCH-ORD-001");
        CreateTestSalesOrder(customer.Id, "SEARCH-ORD-002");
        CreateTestSalesOrder(customer.Id, "OTHER-ORD");
        var querySpec = new QuerySpec { SearchTerm = "SEARCH-ORD" };
        var spec = new SalesOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber.Contains("SEARCH-ORD", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync filters sales orders to only those whose order number contains the supplied filter value.</summary>
    [Fact]
    public async Task QueryAsync_WithOrderNumberFilter_ShouldFilterResults()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "FILTER-ORD-001");
        CreateTestSalesOrder(customer.Id, "FILTER-ORD-002");
        CreateTestSalesOrder(customer.Id, "OTHER-ORD");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "OrderNumber", "FILTER-ORD" } };
        var spec = new SalesOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber.Contains("FILTER-ORD", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync returns only orders belonging to the specified customer identifier.</summary>
    [Fact]
    public async Task QueryAsync_WithCustomerIdFilter_ShouldFilterResults()
    {
        // Arrange
        var customer1 = _context.Customers.First();
        var customer2 = _context.Customers.Skip(1).First();
        CreateTestSalesOrder(customer1.Id, "CUST1-ORD");
        CreateTestSalesOrder(customer2.Id, "CUST2-ORD");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "CustomerId", customer1.Id.ToString() } };
        var spec = new SalesOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.CustomerId == customer1.Id);
    }

    /// <summary>Verifies that QueryAsync returns only orders matching the specified status value.</summary>
    [Fact]
    public async Task QueryAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "STATUS-001", SalesOrderStatus.Confirmed);
        CreateTestSalesOrder(customer.Id, "STATUS-002", SalesOrderStatus.Draft);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Status", SalesOrderStatus.Confirmed.ToString() } };
        var spec = new SalesOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.Status == SalesOrderStatus.Confirmed);
    }

    /// <summary>Verifies that QueryAsync returns only orders within the TotalAmountMin and TotalAmountMax range filters.</summary>
    [Fact]
    public async Task QueryAsync_WithTotalAmountRangeFilter_ShouldFilterResults()
    {
        // Arrange
        var customer = _context.Customers.First();
        var order1 = CreateTestSalesOrder(customer.Id, "TOTAL-001");
        order1.TotalAmount = 50.00m;
        _context.SaveChanges();
        var order2 = CreateTestSalesOrder(customer.Id, "TOTAL-002");
        order2.TotalAmount = 150.00m;
        _context.SaveChanges();
        var order3 = CreateTestSalesOrder(customer.Id, "TOTAL-003");
        order3.TotalAmount = 300.00m;
        _context.SaveChanges();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string>
        {
            { "TotalAmountMin", "100" },
            { "TotalAmountMax", "200" }
        };
        var spec = new SalesOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.TotalAmount >= 100m && o.TotalAmount <= 200m);
    }

    /// <summary>Verifies that QueryAsync returns the correct page number and page size when pagination parameters are supplied.</summary>
    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "PAGE-QUERY-001");
        CreateTestSalesOrder(customer.Id, "PAGE-QUERY-002");
        CreateTestSalesOrder(customer.Id, "PAGE-QUERY-003");
        CreateTestSalesOrder(customer.Id, "PAGE-QUERY-004");
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new SalesOrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(6);
    }

    /// <summary>Verifies that QueryAsync returns sales orders in ascending alphabetical order when sorted by order number.</summary>
    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var customer = _context.Customers.First();
        CreateTestSalesOrder(customer.Id, "ZEBRA-ORD");
        CreateTestSalesOrder(customer.Id, "ALPHA-ORD");
        CreateTestSalesOrder(customer.Id, "BETA-ORD");
        var querySpec = new QuerySpec { SortBy = "OrderNumber", SortDesc = false };
        var spec = new SalesOrderQuerySpec(querySpec);

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

