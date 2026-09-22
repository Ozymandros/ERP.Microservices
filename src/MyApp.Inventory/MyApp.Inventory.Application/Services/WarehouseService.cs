using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Application.Services;

/// <summary>Provides warehouse management operations for the Inventory service.</summary>
public class WarehouseService : AppServiceBase, IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="WarehouseService"/>.</summary>
    /// Initializes a new instance of the WarehouseService class.
    /// <param name="warehouseRepository">The warehouse Repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public WarehouseService(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<WarehouseService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Inventory)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
    }

    /// <summary>Retrieves a warehouse by its unique identifier.</summary>
    /// Gets the warehouse by id asynchronously.
    /// <param name="id">The id.</param>
    /// <returns>The matching <see cref="WarehouseDto"/>, or <c>null</c> if not found.</returns>
    public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        return warehouse == null ? null : _mapper.Map<WarehouseDto>(warehouse);
    }

    /// <summary>Retrieves a warehouse by its name.</summary>
    /// Gets the warehouse by name asynchronously.
    /// <param name="name">The name.</param>
    /// <returns>The matching <see cref="WarehouseDto"/>, or <c>null</c> if not found.</returns>
    public async Task<WarehouseDto?> GetWarehouseByNameAsync(string name)
    {
        var warehouse = await _warehouseRepository.GetByNameAsync(name);
        return warehouse == null ? null : _mapper.Map<WarehouseDto>(warehouse);
    }

    /// <summary>Retrieves all warehouses.</summary>
    /// Gets all warehouses asynchronously.
    public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
    {
        var warehouses = await _warehouseRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<WarehouseDto>>(warehouses);
    }

    /// <summary>Retrieves a paginated list of all warehouses.</summary>
    /// Gets all warehouses paginated asynchronously.
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <returns>A <see cref="PaginatedResult{WarehouseDto}"/> for the requested page.</returns>
    public async Task<PaginatedResult<WarehouseDto>> GetAllWarehousesPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedWarehouses = await _warehouseRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var warehouseDtos = _mapper.Map<IEnumerable<WarehouseDto>>(paginatedWarehouses.Items);
        return new PaginatedResult<WarehouseDto>(warehouseDtos, paginatedWarehouses.PageNumber, paginatedWarehouses.PageSize, paginatedWarehouses.TotalCount);
    }

    /// <summary>Creates a new warehouse from the supplied data.</summary>
    /// Creates a warehouse asynchronously.
    /// <param name="dto">The dto.</param>
    /// <returns>The newly created <see cref="WarehouseDto"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a warehouse with the same name already exists.</exception>
    public async Task<WarehouseDto> CreateWarehouseAsync(CreateUpdateWarehouseDto dto)
    {
        // Check if warehouse with same name already exists
        var existingWarehouse = await _warehouseRepository.GetByNameAsync(dto.Name);
        if (existingWarehouse != null)
        {
            throw new InvalidOperationException($"Warehouse with name '{dto.Name}' already exists.");
        }

        var warehouse = _mapper.Map<Warehouse>(dto);
        var createdWarehouse = await _warehouseRepository.AddAsync(warehouse);
        await SaveChangesAsync();

        return _mapper.Map<WarehouseDto>(createdWarehouse);
    }

    /// <summary>Updates an existing warehouse with the supplied data.</summary>
    /// Updates the warehouse asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <returns>The updated <see cref="WarehouseDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no warehouse with the given identifier is found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the new name is already in use by another warehouse.</exception>
    public async Task<WarehouseDto> UpdateWarehouseAsync(Guid id, CreateUpdateWarehouseDto dto)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID '{id}' not found.");
        }

        // Check if new name is already used by another warehouse
        if (warehouse.Name != dto.Name)
        {
            var existingWarehouse = await _warehouseRepository.GetByNameAsync(dto.Name);
            if (existingWarehouse != null)
            {
                throw new InvalidOperationException($"Warehouse with name '{dto.Name}' already exists.");
            }
        }

        _mapper.Map(dto, warehouse);
        var updatedWarehouse = await _warehouseRepository.UpdateAsync(warehouse);
        await SaveChangesAsync();

        return _mapper.Map<WarehouseDto>(updatedWarehouse);
    }

    /// <summary>Deletes the warehouse with the specified identifier.</summary>
    /// Deletes the warehouse asynchronously.
    /// <param name="id">The id.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no warehouse with the given identifier is found.</exception>
    public async Task DeleteWarehouseAsync(Guid id)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID '{id}' not found.");
        }

        await _warehouseRepository.DeleteAsync(warehouse);
        await SaveChangesAsync();
    }

    /// <summary>Queries warehouses using a specification that encapsulates filtering, sorting and pagination.</summary>
    /// Query warehouses asynchronously.
    /// <param name="spec">The spec.</param>
    /// <returns>A <see cref="PaginatedResult{WarehouseDto}"/> matching the specification.</returns>
    public async Task<PaginatedResult<WarehouseDto>> QueryWarehousesAsync(ISpecification<Warehouse> spec)
    {
        var result = await _warehouseRepository.QueryAsync(spec);
        var dtos = result.Items.Select(w => _mapper.Map<WarehouseDto>(w)).ToList();
        return new PaginatedResult<WarehouseDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
