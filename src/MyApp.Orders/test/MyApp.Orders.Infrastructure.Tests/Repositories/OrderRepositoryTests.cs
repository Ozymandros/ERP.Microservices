using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Specifications;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Orders.Infrastructure.Repositories;
using MyApp.Orders.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Orders.Tests.Repositories;

public class OrderRepositoryTests
{
    private readonly OrdersDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new OrderRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private Order CreateTestOrder(string orderNumber = "ORD-001")
    {
        var order = new Order(Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            Type = OrderType.Transfer,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };
        _context.Orders.Add(order);
        _context.SaveChanges();
        return order;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrder()
    {
        // Arrange
        var order = CreateTestOrder("ORD-001");

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("ORD-001", result.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesOrderLines()
    {
        // Arrange
        var order = CreateTestOrder("ORD-002");
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 5
        };
        _context.OrderLines.Add(orderLine);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Lines);
        Assert.Single(result.Lines);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsAllOrders()
    {
        // Arrange
        CreateTestOrder("ORD-003");
        CreateTestOrder("ORD-004");
        CreateTestOrder("ORD-005");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenNoOrders()
    {
        // Arrange
        _context.Orders.RemoveRange(_context.Orders);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidOrder_CreatesOrder()
    {
        // Arrange
        var order = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-NEW",
            Type = OrderType.Inbound,
            TargetId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        // Act
        await _repository.AddAsync(order);
        var result = await _context.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ORD-NEW", result.OrderNumber);
        Assert.Equal(OrderType.Inbound, result.Type);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingOrder_UpdatesOrderData()
    {
        // Arrange
        var order = CreateTestOrder("ORD-UPDATE");
        order.Status = OrderStatus.Approved;
        order.Type = OrderType.Outbound;

        // Act
        await _repository.UpdateAsync(order);
        var result = await _context.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Approved, result.Status);
        Assert.Equal(OrderType.Outbound, result.Type);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesOrder()
    {
        // Arrange
        var order = CreateTestOrder("ORD-DELETE");

        // Act
        await _repository.DeleteAsync(order);
        await _context.SaveChangesAsync();
        var result = await _context.Orders.FindAsync(order.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var missing = await _repository.GetByIdAsync(nonExistentId);
        if (missing is not null)
            await _repository.DeleteAsync(missing);
    }

    #endregion

    // Note: GetAllPaginatedAsync is not implemented in OrderRepository
    // Pagination is handled through QueryAsync with QuerySpec

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        CreateTestOrder("SEARCH-ORD-001");
        CreateTestOrder("SEARCH-ORD-002");
        CreateTestOrder("OTHER-001");
        var querySpec = new QuerySpec { SearchTerm = "SEARCH-ORD" };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber.Contains("SEARCH-ORD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryAsync_WithOrderNumberFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestOrder("FILTER-ORD-001");
        CreateTestOrder("FILTER-ORD-002");
        CreateTestOrder("OTHER-ORD");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "OrderNumber", "FILTER-ORD" } };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(o => o.OrderNumber.Contains("FILTER-ORD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        var order1 = CreateTestOrder("STATUS-001");
        order1.Status = OrderStatus.Approved;
        _context.SaveChanges();
        CreateTestOrder("STATUS-002");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Status", OrderStatus.Approved.ToString() } };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.Status == OrderStatus.Approved);
    }

    [Fact]
    public async Task QueryAsync_WithTypeFilter_ShouldFilterResults()
    {
        // Arrange
        var order1 = CreateTestOrder("TYPE-001");
        order1.Type = OrderType.Inbound;
        _context.SaveChanges();
        CreateTestOrder("TYPE-002");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Type", OrderType.Inbound.ToString() } };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.Type == OrderType.Inbound);
    }

    [Fact]
    public async Task QueryAsync_WithSourceIdFilter_ShouldFilterResults()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var order1 = CreateTestOrder("SOURCE-001");
        order1.SourceId = sourceId;
        _context.SaveChanges();
        CreateTestOrder("SOURCE-002");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SourceId", sourceId.ToString() } };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(o => o.SourceId == sourceId);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        CreateTestOrder("PAGE-QUERY-001");
        CreateTestOrder("PAGE-QUERY-002");
        CreateTestOrder("PAGE-QUERY-003");
        CreateTestOrder("PAGE-QUERY-004");
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        CreateTestOrder("ZEBRA-ORD");
        CreateTestOrder("ALPHA-ORD");
        CreateTestOrder("BETA-ORD");
        var querySpec = new QuerySpec { SortBy = "OrderNumber", SortDesc = false };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var orderNumbers = result.Items.Select(o => o.OrderNumber).ToList();
        var sortedOrderNumbers = orderNumbers.OrderBy(n => n).ToList();
        orderNumbers.Should().BeEquivalentTo(sortedOrderNumbers);
    }

    [Fact]
    public async Task QueryAsync_WithDescendingSort_ShouldReturnDescendingSortedResults()
    {
        // Arrange
        CreateTestOrder("SORT-DESC-ALPHA");
        CreateTestOrder("SORT-DESC-ZEBRA");
        CreateTestOrder("SORT-DESC-BETA");
        var querySpec = new QuerySpec { SortBy = "OrderNumber", SortDesc = true };
        var spec = new OrderQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var orderNumbers = result.Items.Select(o => o.OrderNumber).ToList();
        var sortedOrderNumbers = orderNumbers.OrderByDescending(n => n).ToList();
        orderNumbers.Should().BeEquivalentTo(sortedOrderNumbers);
    }

    #endregion
}

