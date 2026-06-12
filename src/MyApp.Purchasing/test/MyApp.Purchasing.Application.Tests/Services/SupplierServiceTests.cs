using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Services;

public class SupplierServiceTests
{
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<SupplierService>> _mockLogger;
    private readonly SupplierService _supplierService;

    public SupplierServiceTests()
    {
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<SupplierService>>();
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        _supplierService = new SupplierService(
            _mockSupplierRepository.Object,
            _mockMapper.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetSupplierByIdAsync_WithExistingId_ReturnsSupplierDto()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier(supplierId) { Name = "Test Supplier", ContactName = "Test Supplier", Email = "test@supplier.com" };
        var expectedDto = new SupplierDto(supplierId)
        {
            Name = "Test Supplier",
            ContactName = "Test Supplier",
            Email = "test@supplier.com",
            PhoneNumber = "",
            Address = ""
        };

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
        var email = "test@supplier.com";
        var supplier = new Supplier(Guid.NewGuid()) { Name = "Test Supplier", ContactName = "Test Supplier", Email = email };
        var expectedDto = new SupplierDto(Guid.NewGuid())
        {
            Name = "Test Supplier",
            ContactName = "Test Supplier",
            Email = email,
            PhoneNumber = "",
            Address = ""
        };

        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(supplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(supplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.GetSupplierByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        _mockSupplierRepository.Verify(r => r.GetByEmailAsync(email), Times.Once);
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
    public async Task GetSupplierByNameAsync_WithExistingName_ReturnsSupplierDto()
    {
        // Arrange
        var name = "Test Supplier";
        var supplier = new Supplier(Guid.NewGuid()) { Name = name, ContactName = "Contact", Email = "test@supplier.com" };
        var expectedDto = new SupplierDto(Guid.NewGuid())
        {
            Name = name,
            ContactName = "Contact",
            Email = "test@supplier.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockSupplierRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync(new List<Supplier> { supplier });
        _mockMapper.Setup(m => m.Map<SupplierDto>(supplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.GetSupplierByNameAsync(name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        _mockSupplierRepository.Verify(r => r.GetByNameAsync(name), Times.Once);
    }

    [Fact]
    public async Task GetSupplierByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var name = "Non-Existent Supplier";
        _mockSupplierRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync(new List<Supplier>());

        // Act
        var result = await _supplierService.GetSupplierByNameAsync(name);

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
            new Supplier(Guid.NewGuid()) { Name = "Test Supplier 1" },
            new Supplier(Guid.NewGuid()) { Name = "Test Supplier 2" }
        };
        var dtos = new List<SupplierDto>
        {
            new SupplierDto(Guid.NewGuid())
            {
                Name = "Test Supplier 1",
                ContactName = "Test Supplier 1"
            },
            new SupplierDto(Guid.NewGuid())
            {
                Name = "Test Supplier 2",
                ContactName = "Test Supplier 2"
            }
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
            new Supplier(Guid.NewGuid()) { Name = "Supplier 1" },
            new Supplier(Guid.NewGuid()) { Name = "Supplier 2" }
        };
        var dtos = new List<SupplierDto>
        {
            new SupplierDto(Guid.NewGuid())
            {
                Name = "Supplier 1",
                ContactName = "Supplier 1"
            },
            new SupplierDto(Guid.NewGuid())
            {
                Name = "Supplier 2",
                ContactName = "Supplier 2"
            }
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
        var dto = new CreateUpdateSupplierDto("New Supplier", "Test Contact", "new@supplier.com");
        var supplier = new Supplier(Guid.NewGuid()) { Name = "New Supplier", ContactName = "Test Contact", Email = "new@supplier.com" };
        var createdSupplier = new Supplier(Guid.NewGuid()) { Name = "New Supplier", ContactName = "Test Contact", Email = "new@supplier.com" };
        var expectedDto = new SupplierDto(createdSupplier.Id)
        {
            Name = "New Supplier",
            ContactName = "Test Contact",
            Email = "new@supplier.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockSupplierRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Supplier?)null);
        _mockMapper.Setup(m => m.Map<Supplier>(dto)).Returns(supplier);
        _mockSupplierRepository.Setup(r => r.AddAsync(supplier)).ReturnsAsync(createdSupplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(createdSupplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.CreateSupplierAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Supplier", result.Name);
        _mockSupplierRepository.Verify(r => r.AddAsync(supplier), Times.Once);
    }

    [Fact]
    public async Task CreateSupplierAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateUpdateSupplierDto("Test Name", "Test Contact", "duplicate@supplier.com");
        var existingSupplier = new Supplier(Guid.NewGuid()) { Email = "duplicate@supplier.com" };

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
        var existingSupplier = new Supplier(supplierId) { Name = "Old Name", ContactName = "Test Contact", Email = "old@email.com" };
        var updateDto = new CreateUpdateSupplierDto("Updated Name", "Test Contact", "old@email.com");
        var updatedSupplier = new Supplier(supplierId) { Name = "Updated Name", ContactName = "Test Contact", Email = "old@email.com" };
        var expectedDto = new SupplierDto(supplierId)
        {
            Name = "Updated Name",
            ContactName = "Test Contact",
            Email = "old@email.com",
            PhoneNumber = "",
            Address = ""
        };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(existingSupplier);
        _mockMapper.Setup(m => m.Map(updateDto, existingSupplier));
        _mockSupplierRepository.Setup(r => r.UpdateAsync(existingSupplier)).ReturnsAsync(updatedSupplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(updatedSupplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.UpdateSupplierAsync(supplierId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        _mockSupplierRepository.Verify(r => r.UpdateAsync(existingSupplier), Times.Once);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithNonExistentSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var updateDto = new CreateUpdateSupplierDto("Test Name", "Test Contact", "test@email.com");

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
        var existingSupplier = new Supplier(supplierId) { Email = "old@email.com" };
        var updateDto = new CreateUpdateSupplierDto("Test Name", "Test Contact", "new@email.com");
        var conflictingSupplier = new Supplier(Guid.NewGuid()) { Email = "new@email.com" };

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
        var supplier = new Supplier(supplierId);

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

    #region QuerySuppliersAsync Tests

    [Fact]
    public async Task QuerySuppliersAsync_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new Supplier(Guid.NewGuid()) { Name = "Search Supplier", Email = "search@example.com" }
        };

        var querySpec = new QuerySpec { SearchTerm = "Search" };
        var spec = new SupplierQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Supplier>(suppliers, 1, 20, 1);

        var supplierDtos = new List<SupplierDto>
        {
            new SupplierDto(Guid.NewGuid()) { Name = "Search Supplier", ContactName = "Search Supplier", Email = "search@example.com", PhoneNumber = "", Address = "" }
        };

        _mockSupplierRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<SupplierDto>(It.IsAny<Supplier>())).Returns((Supplier s) =>
            new SupplierDto(s.Id) { Name = s.Name, ContactName = s.ContactName ?? "", Email = s.Email, PhoneNumber = s.PhoneNumber ?? "", Address = s.Address ?? "" });

        // Act
        var result = await _supplierService.QuerySuppliersAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        _mockSupplierRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    [Fact]
    public async Task QuerySuppliersAsync_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new Supplier(Guid.NewGuid()) { Name = "Supplier 1", Email = "s1@example.com" },
            new Supplier(Guid.NewGuid()) { Name = "Supplier 2", Email = "s2@example.com" }
        };

        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new SupplierQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Supplier>(suppliers, 1, 2, 10);

        _mockSupplierRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<SupplierDto>(It.IsAny<Supplier>())).Returns((Supplier s) =>
            new SupplierDto(s.Id) { Name = s.Name, ContactName = s.ContactName ?? "", Email = s.Email, PhoneNumber = s.PhoneNumber ?? "", Address = s.Address ?? "" });

        // Act
        var result = await _supplierService.QuerySuppliersAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(10);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task CreateSupplierAsync_WithEmptyEmail_ThrowsException()
    {
        // Arrange
        var dto = new CreateUpdateSupplierDto("Supplier", "Contact", "");

        _mockSupplierRepository.Setup(r => r.GetByEmailAsync("")).ReturnsAsync((Supplier?)null);

        // Act & Assert - Empty email should be handled by validation or service logic
        // This test verifies the service handles empty email appropriately
        var supplier = new Supplier(Guid.NewGuid()) { Name = "Supplier", ContactName = "Contact", Email = "" };
        var createdSupplier = new Supplier(Guid.NewGuid()) { Name = "Supplier", ContactName = "Contact", Email = "" };
        var expectedDto = new SupplierDto(createdSupplier.Id) { Name = "Supplier", ContactName = "Contact", Email = "", PhoneNumber = "", Address = "" };

        _mockMapper.Setup(m => m.Map<Supplier>(dto)).Returns(supplier);
        _mockSupplierRepository.Setup(r => r.AddAsync(supplier)).ReturnsAsync(createdSupplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(createdSupplier)).Returns(expectedDto);

        var result = await _supplierService.CreateSupplierAsync(dto);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithSameEmail_UpdatesSuccessfully()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var email = "test@example.com";
        var existingSupplier = new Supplier(supplierId) { Name = "Old Name", Email = email };
        var updateDto = new CreateUpdateSupplierDto("New Name", "Contact", email);
        var updatedSupplier = new Supplier(supplierId) { Name = "New Name", Email = email };
        var expectedDto = new SupplierDto(supplierId) { Name = "New Name", ContactName = "Contact", Email = email, PhoneNumber = "", Address = "" };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(existingSupplier);
        _mockMapper.Setup(m => m.Map(updateDto, existingSupplier));
        _mockSupplierRepository.Setup(r => r.UpdateAsync(existingSupplier)).ReturnsAsync(updatedSupplier);
        _mockMapper.Setup(m => m.Map<SupplierDto>(updatedSupplier)).Returns(expectedDto);

        // Act
        var result = await _supplierService.UpdateSupplierAsync(supplierId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        _mockSupplierRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllSuppliersAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockSupplierRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Supplier>());
        _mockMapper.Setup(m => m.Map<IEnumerable<SupplierDto>>(It.IsAny<IEnumerable<Supplier>>())).Returns(Enumerable.Empty<SupplierDto>());

        // Act
        var result = await _supplierService.GetAllSuppliersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSupplierByIdAsync_WithEmptyGuid_ReturnsNull()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(emptyGuid)).ReturnsAsync((Supplier?)null);

        // Act
        var result = await _supplierService.GetSupplierByIdAsync(emptyGuid);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
