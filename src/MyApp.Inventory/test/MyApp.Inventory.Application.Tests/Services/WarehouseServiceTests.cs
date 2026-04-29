using AutoMapper;
using FluentAssertions;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
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
        var expectedDto = new WarehouseDto(Guid.NewGuid())
        {
            Name = "Main Warehouse",
            Location = ""
        };

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
            new WarehouseDto(Guid.NewGuid())
            {
                Name = "Warehouse 1",
                Location = ""
            },
            new WarehouseDto(Guid.NewGuid())
            {
                Name = "Warehouse 2",
                Location = ""
            }
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
        var expectedDto = new WarehouseDto(Guid.NewGuid())
        {
            Name = "New Warehouse",
            Location = ""
        };

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
        var expectedDto = new WarehouseDto(Guid.NewGuid())
        {
            Name = "Old Name",
            Location = ""
        };

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

    #region GetAllWarehousesPaginatedAsync Tests

    [Fact]
    public async Task GetAllWarehousesPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 1" },
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 2" },
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 3" }
        };

        var paginatedWarehouses = new PaginatedResult<Warehouse>(
            warehouses.Take(2),
            1,
            2,
            3
        );

        var warehouseDtos = new List<WarehouseDto>
        {
            new WarehouseDto(Guid.NewGuid()) { Name = "Warehouse 1", Location = "" },
            new WarehouseDto(Guid.NewGuid()) { Name = "Warehouse 2", Location = "" }
        };

        _mockWarehouseRepository.Setup(r => r.GetAllPaginatedAsync(1, 2)).ReturnsAsync(paginatedWarehouses);
        _mockMapper.Setup(m => m.Map<IEnumerable<WarehouseDto>>(It.IsAny<IEnumerable<Warehouse>>())).Returns(warehouseDtos);

        // Act
        var result = await _warehouseService.GetAllWarehousesPaginatedAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        _mockWarehouseRepository.Verify(r => r.GetAllPaginatedAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task GetAllWarehousesPaginatedAsync_WithEmptyResult_ReturnsEmptyPaginatedResult()
    {
        // Arrange
        var paginatedWarehouses = new PaginatedResult<Warehouse>(
            Enumerable.Empty<Warehouse>(),
            1,
            20,
            0
        );

        _mockWarehouseRepository.Setup(r => r.GetAllPaginatedAsync(1, 20)).ReturnsAsync(paginatedWarehouses);
        _mockMapper.Setup(m => m.Map<IEnumerable<WarehouseDto>>(It.IsAny<IEnumerable<Warehouse>>())).Returns(Enumerable.Empty<WarehouseDto>());

        // Act
        var result = await _warehouseService.GetAllWarehousesPaginatedAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region QueryWarehousesAsync Tests

    [Fact]
    public async Task QueryWarehousesAsync_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse(Guid.NewGuid()) { Name = "Main Warehouse", Location = "City" }
        };

        var querySpec = new QuerySpec { SearchTerm = "Main" };
        var spec = new WarehouseQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Warehouse>(warehouses, 1, 20, 1);

        var warehouseDtos = new List<WarehouseDto>
        {
            new WarehouseDto(Guid.NewGuid()) { Name = "Main Warehouse", Location = "City" }
        };

        _mockWarehouseRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(It.IsAny<Warehouse>())).Returns((Warehouse w) =>
            new WarehouseDto(w.Id) { Name = w.Name, Location = w.Location });

        // Act
        var result = await _warehouseService.QueryWarehousesAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        _mockWarehouseRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    [Fact]
    public async Task QueryWarehousesAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse", Location = "Test City" }
        };

        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "City", "Test City" } };
        var spec = new WarehouseQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Warehouse>(warehouses, 1, 20, 1);

        _mockWarehouseRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(It.IsAny<Warehouse>())).Returns((Warehouse w) =>
            new WarehouseDto(w.Id) { Name = w.Name, Location = w.Location });

        // Act
        var result = await _warehouseService.QueryWarehousesAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _mockWarehouseRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task CreateWarehouseAsync_WithEmptyName_CreatesWarehouse()
    {
        // Arrange
        var dto = new CreateUpdateWarehouseDto("", "Location");
        var warehouse = new Warehouse(Guid.NewGuid()) { Name = "", Location = "Location" };
        var expectedDto = new WarehouseDto(Guid.NewGuid()) { Name = "", Location = "Location" };

        _mockWarehouseRepository.Setup(r => r.GetByNameAsync("")).ReturnsAsync((Warehouse?)null);
        _mockMapper.Setup(m => m.Map<Warehouse>(dto)).Returns(warehouse);
        _mockWarehouseRepository.Setup(r => r.AddAsync(warehouse)).ReturnsAsync(warehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(warehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.CreateWarehouseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("");
    }

    [Fact]
    public async Task CreateWarehouseAsync_WithSpecialCharactersInName_CreatesWarehouse()
    {
        // Arrange
        var name = "Warehouse!@#$%^&*()";
        var dto = new CreateUpdateWarehouseDto(name, "Location");
        var warehouse = new Warehouse(Guid.NewGuid()) { Name = name, Location = "Location" };
        var expectedDto = new WarehouseDto(Guid.NewGuid()) { Name = name, Location = "Location" };

        _mockWarehouseRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync((Warehouse?)null);
        _mockMapper.Setup(m => m.Map<Warehouse>(dto)).Returns(warehouse);
        _mockWarehouseRepository.Setup(r => r.AddAsync(warehouse)).ReturnsAsync(warehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(warehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.CreateWarehouseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
    }

    [Fact]
    public async Task CreateWarehouseAsync_WithVeryLongName_CreatesWarehouse()
    {
        // Arrange
        var longName = new string('A', 500);
        var dto = new CreateUpdateWarehouseDto(longName, "Location");
        var warehouse = new Warehouse(Guid.NewGuid()) { Name = longName, Location = "Location" };
        var expectedDto = new WarehouseDto(Guid.NewGuid()) { Name = longName, Location = "Location" };

        _mockWarehouseRepository.Setup(r => r.GetByNameAsync(longName)).ReturnsAsync((Warehouse?)null);
        _mockMapper.Setup(m => m.Map<Warehouse>(dto)).Returns(warehouse);
        _mockWarehouseRepository.Setup(r => r.AddAsync(warehouse)).ReturnsAsync(warehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(warehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.CreateWarehouseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(longName);
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WithSameName_UpdatesSuccessfully()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var name = "Warehouse";
        var existingWarehouse = new Warehouse(warehouseId) { Name = name };
        var updateDto = new CreateUpdateWarehouseDto(name, "New Location");
        var updatedWarehouse = new Warehouse(warehouseId) { Name = name, Location = "New Location" };
        var expectedDto = new WarehouseDto(Guid.NewGuid()) { Name = name, Location = "New Location" };

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(existingWarehouse);
        _mockMapper.Setup(m => m.Map(updateDto, existingWarehouse));
        _mockWarehouseRepository.Setup(r => r.UpdateAsync(existingWarehouse)).ReturnsAsync(updatedWarehouse);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(updatedWarehouse)).Returns(expectedDto);

        // Act
        var result = await _warehouseService.UpdateWarehouseAsync(warehouseId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Location.Should().Be("New Location");
        _mockWarehouseRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllWarehousesAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockWarehouseRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Warehouse>());
        _mockMapper.Setup(m => m.Map<IEnumerable<WarehouseDto>>(It.IsAny<IEnumerable<Warehouse>>())).Returns(Enumerable.Empty<WarehouseDto>());

        // Act
        var result = await _warehouseService.GetAllWarehousesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateWarehouseAsync_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var dto = new CreateUpdateWarehouseDto("Warehouse", "Location");
        var warehouse = new Warehouse(Guid.NewGuid()) { Name = "Warehouse", Location = "Location" };

        _mockWarehouseRepository.Setup(r => r.GetByNameAsync(dto.Name)).ReturnsAsync((Warehouse?)null);
        _mockMapper.Setup(m => m.Map<Warehouse>(dto)).Returns(warehouse);
        _mockWarehouseRepository.Setup(r => r.AddAsync(warehouse)).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _warehouseService.CreateWarehouseAsync(dto));
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WhenMapperThrowsException_PropagatesException()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var existingWarehouse = new Warehouse(warehouseId) { Name = "Old Name" };
        var updateDto = new CreateUpdateWarehouseDto("New Name", "Location");

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(existingWarehouse);
        _mockMapper.Setup(m => m.Map(updateDto, existingWarehouse)).Throws(new Exception("Mapping error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _warehouseService.UpdateWarehouseAsync(warehouseId, updateDto));
    }

    [Fact]
    public async Task QueryWarehousesAsync_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 1" },
            new Warehouse(Guid.NewGuid()) { Name = "Warehouse 2" }
        };

        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new WarehouseQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Warehouse>(warehouses, 1, 2, 10);

        _mockWarehouseRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<WarehouseDto>(It.IsAny<Warehouse>())).Returns((Warehouse w) =>
            new WarehouseDto(w.Id) { Name = w.Name, Location = w.Location });

        // Act
        var result = await _warehouseService.QueryWarehousesAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task GetWarehouseByIdAsync_WithEmptyGuid_ReturnsNull()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(emptyGuid)).ReturnsAsync((Warehouse?)null);

        // Act
        var result = await _warehouseService.GetWarehouseByIdAsync(emptyGuid);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteWarehouseAsync_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var warehouse = new Warehouse(warehouseId) { Name = "Warehouse" };

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(warehouse);
        _mockWarehouseRepository.Setup(r => r.DeleteAsync(warehouse)).ThrowsAsync(new Exception("Delete error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _warehouseService.DeleteWarehouseAsync(warehouseId));
    }

    #endregion
}
