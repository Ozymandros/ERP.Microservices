using AutoMapper;
using Moq;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Services;

public class SupplierServiceTests
{
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SupplierService _supplierService;

    public SupplierServiceTests()
    {
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockMapper = new Mock<IMapper>();

        _supplierService = new SupplierService(
            _mockSupplierRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetSupplierByIdAsync_WithExistingId_ReturnsSupplierDto()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier { Id = supplierId, Name = "Test Supplier", Email = "test@supplier.com" };
        var expectedDto = new SupplierDto { Name = "Test Supplier", Email = "test@supplier.com" };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(supplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(supplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.GetSupplierByIdAsync(supplierId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Supplier", result.Name);
        _mockSupplierRepository.Verify(r => r.GetByIdAsync(supplierId), Times.Once);
    }

    [Fact]
    public async Task GetSupplierByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync((Supplier?)null);

        // Act
        var result = await _supplierService.GetSupplierByIdAsync(supplierId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSupplierByEmailAsync_WithExistingEmail_ReturnsSupplierDto()
    {
        // Arrange
        var email = "supplier@example.com";
        var supplier = new Supplier { Email = email, Name = "Test Supplier" };
        var expectedDto = new SupplierDto { Email = email };

        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(supplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(supplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.GetSupplierByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task GetSupplierByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((Supplier?)null);

        // Act
        var result = await _supplierService.GetSupplierByEmailAsync(email);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSuppliersByNameAsync_ReturnsMatchingSuppliers()
    {
        // Arrange
        var name = "Test";
        var suppliers = new List<Supplier>
        {
            new Supplier { Name = "Test Supplier 1" },
            new Supplier { Name = "Test Supplier 2" }
        };
        var dtos = new List<SupplierDto>
        {
            new SupplierDto { Name = "Test Supplier 1" },
            new SupplierDto { Name = "Test Supplier 2" }
        };

        _mockSupplierRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync(suppliers);
        _mockMapper.Setup(m => m.Map<IEnumerable<SupplierDto>>(suppliers)).Returns(dtos);

        // Act
        var result = await _supplierService.GetSuppliersByNameAsync(name);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllSuppliersAsync_ReturnsAllSuppliers()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new Supplier { Name = "Supplier 1" },
            new Supplier { Name = "Supplier 2" }
        };
        var dtos = new List<SupplierDto>
        {
            new SupplierDto { Name = "Supplier 1" },
            new SupplierDto { Name = "Supplier 2" }
        };

        _mockSupplierRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(suppliers);
        _mockMapper.Setup(m => m.Map<IEnumerable<SupplierDto>>(suppliers)).Returns(dtos);

        // Act
        var result = await _supplierService.GetAllSuppliersAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _mockSupplierRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateSupplierAsync_WithUniqueEmail_CreatesSupplier()
    {
        // Arrange
        var dto = new CreateUpdateSupplierDto { Name = "New Supplier", Email = "new@supplier.com" };
        var supplier = new Supplier { Name = "New Supplier", Email = "new@supplier.com" };
        var createdSupplier = new Supplier { Id = Guid.NewGuid(), Name = "New Supplier", Email = "new@supplier.com" };
        var expectedDto = new SupplierDto { Name = "New Supplier" };

        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Supplier?)null);
        _mockMapper.Setup(m => m.Map<Supplier>(dto)).Returns(supplier);
        _mockSupplierRepository.Setup(r => r.AddAsync(supplier)).ReturnsAsync(createdSupplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(createdSupplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.CreateSupplierAsync(dto);

        // Assert
        Assert.NotNull(result);
        _mockSupplierRepository.Verify(r => r.AddAsync(supplier), Times.Once);
    }

    [Fact]
    public async Task CreateSupplierAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateUpdateSupplierDto { Email = "duplicate@supplier.com" };
        var existingSupplier = new Supplier { Email = "duplicate@supplier.com" };

        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(existingSupplier);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _supplierService.CreateSupplierAsync(dto));

        Assert.Contains("already exists", exception.Message);
        _mockSupplierRepository.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithExistingSupplier_UpdatesSuccessfully()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var existingSupplier = new Supplier { Id = supplierId, Email = "old@email.com" };
        var updateDto = new CreateUpdateSupplierDto { Email = "old@email.com", Name = "Updated Name" };
        var updatedSupplier = new Supplier { Id = supplierId, Email = "old@email.com", Name = "Updated Name" };
        var expectedDto = new SupplierDto { Name = "Updated Name" };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(existingSupplier);
        _mockMapper.Setup(m => m.Map(updateDto, existingSupplier));
        _mockSupplierRepository.Setup(r => r.UpdateAsync(existingSupplier)).ReturnsAsync(updatedSupplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(updatedSupplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.UpdateSupplierAsync(supplierId, updateDto);

        // Assert
        Assert.NotNull(result);
        _mockSupplierRepository.Verify(r => r.UpdateAsync(existingSupplier), Times.Once);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithNonExistentSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var updateDto = new CreateUpdateSupplierDto();

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync((Supplier?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _supplierService.UpdateSupplierAsync(supplierId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockSupplierRepository.Verify(r => r.UpdateAsync(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var existingSupplier = new Supplier { Id = supplierId, Email = "old@email.com" };
        var updateDto = new CreateUpdateSupplierDto { Email = "new@email.com" };
        var conflictingSupplier = new Supplier { Id = Guid.NewGuid(), Email = "new@email.com" };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(existingSupplier);
        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(updateDto.Email)).ReturnsAsync(conflictingSupplier);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _supplierService.UpdateSupplierAsync(supplierId, updateDto));

        Assert.Contains("already exists", exception.Message);
        _mockSupplierRepository.Verify(r => r.UpdateAsync(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSupplierAsync_WithExistingSupplier_DeletesSupplier()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier { Id = supplierId };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(supplier);

        // Act
        await _supplierService.DeleteSupplierAsync(supplierId);

        // Assert
        _mockSupplierRepository.Verify(r => r.DeleteAsync(supplier), Times.Once);
    }

    [Fact]
    public async Task DeleteSupplierAsync_WithNonExistentSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync((Supplier?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _supplierService.DeleteSupplierAsync(supplierId));

        Assert.Contains("not found", exception.Message);
        _mockSupplierRepository.Verify(r => r.DeleteAsync(It.IsAny<Supplier>()), Times.Never);
    }
}
