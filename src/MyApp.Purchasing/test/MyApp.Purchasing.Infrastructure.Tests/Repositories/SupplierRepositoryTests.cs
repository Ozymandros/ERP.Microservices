using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Purchasing.Tests.Helpers;
using Xunit;

namespace MyApp.Purchasing.Tests.Repositories;

public class SupplierRepositoryTests
{
    private readonly PurchasingDbContext _context;
    private readonly SupplierRepository _repository;

    public SupplierRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new SupplierRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private Supplier CreateTestSupplier(string name = "Test Supplier", string email = "test@supplier.com")
    {
        var supplier = new Supplier(Guid.NewGuid())
        {
            Name = name,
            ContactName = "Contact Person",
            Email = email,
            PhoneNumber = "555-1234",
            Address = "123 Supplier St"
        };
        _context.Suppliers.Add(supplier);
        _context.SaveChanges();
        return supplier;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSupplier()
    {
        // Arrange
        var supplier = CreateTestSupplier("ABC Suppliers", "abc@supplier.com");

        // Act
        var result = await _repository.GetByIdAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(supplier.Id, result.Id);
        Assert.Equal("ABC Suppliers", result.Name);
        Assert.Equal("abc@supplier.com", result.Email);
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

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsSupplier()
    {
        // Arrange
        CreateTestSupplier("XYZ Suppliers", "xyz@supplier.com");

        // Act
        var result = await _repository.GetByEmailAsync("xyz@supplier.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("xyz@supplier.com", result.Email);
        Assert.Equal("XYZ Suppliers", result.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@supplier.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithPartialName_ReturnsMatchingSuppliers()
    {
        // Arrange
        CreateTestSupplier("Global Suppliers Inc", "global@supplier.com");
        CreateTestSupplier("Global Trade Co", "globaltrade@supplier.com");
        CreateTestSupplier("Local Distributors", "local@supplier.com");

        // Act
        var result = await _repository.GetByNameAsync("Global");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.Contains("Global", s.Name));
    }

    [Fact]
    public async Task GetByNameAsync_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        CreateTestSupplier("ABC Suppliers", "abc@supplier.com");

        // Act
        var result = await _repository.GetByNameAsync("NonExistent");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidSupplier_CreatesSupplier()
    {
        // Arrange
        var supplier = new Supplier(Guid.NewGuid())
        {
            Name = "New Supplier",
            ContactName = "John Doe",
            Email = "new@supplier.com",
            PhoneNumber = "555-9999",
            Address = "456 New St"
        };

        // Act
        await _repository.AddAsync(supplier);
        var result = await _context.Suppliers.FindAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Supplier", result.Name);
        Assert.Equal("new@supplier.com", result.Email);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingSupplier_UpdatesSupplierData()
    {
        // Arrange
        var supplier = CreateTestSupplier("Original Name", "original@supplier.com");
        supplier.Name = "Updated Name";
        supplier.Email = "updated@supplier.com";

        // Act
        await _repository.UpdateAsync(supplier);
        var result = await _context.Suppliers.FindAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("updated@supplier.com", result.Email);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesSupplier()
    {
        // Arrange
        var supplier = CreateTestSupplier("Delete Me", "delete@supplier.com");

        // Act
        await _repository.DeleteAsync(supplier);
        var result = await _context.Suppliers.FindAsync(supplier.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllSuppliers()
    {
        // Arrange
        CreateTestSupplier("Supplier 1", "supplier1@example.com");
        CreateTestSupplier("Supplier 2", "supplier2@example.com");
        CreateTestSupplier("Supplier 3", "supplier3@example.com");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    #endregion
}
