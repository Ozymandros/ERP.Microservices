using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Infrastructure.Data;
using MyApp.Sales.Infrastructure.Data.Repositories;
using MyApp.Sales.Tests.Helpers;
using Xunit;

namespace MyApp.Sales.Tests.Repositories;

public class CustomerRepositoryTests
{
    private readonly SalesDbContext _context;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new CustomerRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private Customer CreateTestCustomer(string name = "Test Customer", string email = "test@example.com")
    {
        var customer = new Customer(Guid.NewGuid())
        {
            Name = name,
            Email = email,
            PhoneNumber = "555-1234",
            Address = "123 Test St"
        };
        _context.Customers.Add(customer);
        _context.SaveChanges();
        return customer;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCustomer()
    {
        // Arrange
        var customer = CreateTestCustomer("John Doe", "john@example.com");

        // Act
        var result = await _repository.GetByIdAsync(customer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
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
    public async Task GetByIdAsync_IncludesOrders()
    {
        // Arrange
        var customer = CreateTestCustomer("Jane Doe", "jane@example.com");
        var order = new SalesOrder(Guid.NewGuid())
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100.00m
        };
        _context.SalesOrders.Add(order);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByIdAsync(customer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Orders);
        Assert.Single(result.Orders);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsAllCustomers()
    {
        // Arrange
        CreateTestCustomer("Customer 1", "customer1@example.com");
        CreateTestCustomer("Customer 2", "customer2@example.com");
        CreateTestCustomer("Customer 3", "customer3@example.com");

        // Act
        var result = await _repository.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenNoCustomers()
    {
        // Arrange
        _context.Customers.RemoveRange(_context.Customers);
        _context.SaveChanges();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidCustomer_CreatesCustomer()
    {
        // Arrange
        var customer = new Customer(Guid.NewGuid())
        {
            Name = "New Customer",
            Email = "new@example.com",
            PhoneNumber = "555-9999",
            Address = "456 New St"
        };

        // Act
        await _repository.AddAsync(customer);
        var result = await _context.Customers.FindAsync(customer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Customer", result.Name);
        Assert.Equal("new@example.com", result.Email);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingCustomer_UpdatesCustomerData()
    {
        // Arrange
        var customer = CreateTestCustomer("Original Name", "original@example.com");
        customer.Name = "Updated Name";
        customer.Email = "updated@example.com";

        // Act
        await _repository.UpdateAsync(customer);
        var result = await _context.Customers.FindAsync(customer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("updated@example.com", result.Email);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesCustomer()
    {
        // Arrange
        var customer = CreateTestCustomer("Delete Me", "delete@example.com");

        // Act
        await _repository.DeleteAsync(customer.Id);
        var result = await _context.Customers.FindAsync(customer.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await _repository.DeleteAsync(nonExistentId); // Should not throw
    }

    #endregion
}
