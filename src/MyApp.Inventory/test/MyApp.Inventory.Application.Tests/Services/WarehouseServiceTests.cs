using AutoMapper;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Services;

public class WarehouseServiceTests
{
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly WarehouseService _warehouseService;

    public WarehouseServiceTests()
    {
        _mockWarehouseRepository = new Mock<IWarehouseRepository>();
        _mockMapper = new Mock<IMapper>();

        _warehouseService = new WarehouseService(
            _mockWarehouseRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetWarehouseByIdAsync_WithExistingId_ReturnsWarehouseDto()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var warehouse = new Warehouse(warehouseId) { Name = "Main Warehouse" };
        var expectedDto = new WarehouseDto(Guid.NewGuid(), default, "", null, null, "Main Warehouse", "");

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(warehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(warehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.GetWarehouseByIdAsync(warehouseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Main Warehouse", result.Name);
        _mockWarehouseRepository.Verify(r => r.GetByIdAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task GetWarehouseByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync((Warehouse?)null);

        // Act
        var result = await _warehouseService.GetWarehouseByIdAsync(warehouseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllWarehousesAsync_ReturnsAllWarehouses()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 1" },
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 2" }
        };

        var warehouseDtos = new List<WarehouseDto>
        {
            new WarehouseDto(Guid.NewGuid(), default, "", null, null, "Warehouse 1", ""),
            new WarehouseDto(Guid.NewGuid(), default, "", null, null, "Warehouse 2", "")
        };

        _mockWarehouseRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(warehouses);
        _mockMapper.Setup(m => m.Map<IEnumerable<WarehouseDto>>(warehouses)).Returns(warehouseDtos);

        // Act
        var result = await _warehouseService.GetAllWarehousesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _mockWarehouseRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateWarehouseAsync_WithUniqueName_CreatesWarehouse()
    {
        // Arrange
        var dto = new CreateUpdateWarehouseDto("New Warehouse", "");
        var warehouse = new Warehouse(Guid.NewGuid()) { Name = "New Warehouse" };
        var createdWarehouse = new Warehouse(Guid.NewGuid()) { Name = "New Warehouse" };
        var expectedDto = new WarehouseDto(Guid.NewGuid(), default, "", null, null, "New Warehouse", "");

        _mockWarehouseRepository.Setup(r => r.GetByNameAsync(dto.Name)).ReturnsAsync((Warehouse?)null);
        _mockMapper.Setup(m => m.Map<Warehouse>(dto)).Returns(warehouse);
        _mockWarehouseRepository.Setup(r => r.AddAsync(warehouse)).ReturnsAsync(createdWarehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(createdWarehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.CreateWarehouseAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Warehouse", result.Name);
        _mockWarehouseRepository.Verify(r => r.AddAsync(warehouse), Times.Once);
    }

    [Fact]
    public async Task CreateWarehouseAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateUpdateWarehouseDto("Existing Warehouse", "");
        var existingWarehouse = new Warehouse(Guid.NewGuid()) { Name = "Existing Warehouse" };

        _mockWarehouseRepository.Setup(r => r.GetByNameAsync(dto.Name)).ReturnsAsync(existingWarehouse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _warehouseService.CreateWarehouseAsync(dto));

        Assert.Contains("already exists", exception.Message);
        _mockWarehouseRepository.Verify(r => r.AddAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WithExistingWarehouse_UpdatesSuccessfully()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var existingWarehouse = new Warehouse(warehouseId) { Name = "Old Name" };
        var updateDto = new CreateUpdateWarehouseDto("Old Name", "");
        var updatedWarehouse = new Warehouse(warehouseId) { Name = "Old Name" };
        var expectedDto = new WarehouseDto(Guid.NewGuid(), default, "", null, null, "Old Name", "");

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(existingWarehouse);
        _mockMapper.Setup(m => m.Map(updateDto, existingWarehouse));
        _mockWarehouseRepository.Setup(r => r.UpdateAsync(existingWarehouse)).ReturnsAsync(updatedWarehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(updatedWarehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.UpdateWarehouseAsync(warehouseId, updateDto);

        // Assert
        Assert.NotNull(result);
        _mockWarehouseRepository.Verify(r => r.UpdateAsync(existingWarehouse), Times.Once);
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WithNonExistentWarehouse_ThrowsKeyNotFoundException()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var updateDto = new CreateUpdateWarehouseDto("Warehouse", "");

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync((Warehouse?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _warehouseService.UpdateWarehouseAsync(warehouseId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockWarehouseRepository.Verify(r => r.UpdateAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var existingWarehouse = new Warehouse(warehouseId) { Name = "Old Name" };
        var updateDto = new CreateUpdateWarehouseDto("New Name", "");
        var conflictingWarehouse = new Warehouse(Guid.NewGuid()) { Name = "New Name" };

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(existingWarehouse);
        _mockWarehouseRepository.Setup(r => r.GetByNameAsync(updateDto.Name)).ReturnsAsync(conflictingWarehouse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _warehouseService.UpdateWarehouseAsync(warehouseId, updateDto));

        Assert.Contains("already exists", exception.Message);
        _mockWarehouseRepository.Verify(r => r.UpdateAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task DeleteWarehouseAsync_WithExistingWarehouse_DeletesWarehouse()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var warehouse = new Warehouse(warehouseId) { Name = "Warehouse" };

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(warehouse);

        // Act
        await _warehouseService.DeleteWarehouseAsync(warehouseId);

        // Assert
        _mockWarehouseRepository.Verify(r => r.DeleteAsync(warehouse), Times.Once);
    }

    [Fact]
    public async Task DeleteWarehouseAsync_WithNonExistentWarehouse_ThrowsKeyNotFoundException()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync((Warehouse?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _warehouseService.DeleteWarehouseAsync(warehouseId));

        Assert.Contains("not found", exception.Message);
        _mockWarehouseRepository.Verify(r => r.DeleteAsync(It.IsAny<Warehouse>()), Times.Never);
    }
}
