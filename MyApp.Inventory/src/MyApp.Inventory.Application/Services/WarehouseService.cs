using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IMapper _mapper;

    public WarehouseService(IWarehouseRepository warehouseRepository, IMapper mapper)
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        return warehouse == null ? null : _mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
    {
        var warehouses = await _warehouseRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<WarehouseDto>>(warehouses);
    }

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

        return _mapper.Map<WarehouseDto>(createdWarehouse);
    }

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

        return _mapper.Map<WarehouseDto>(updatedWarehouse);
    }

    public async Task DeleteWarehouseAsync(Guid id)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID '{id}' not found.");
        }

        await _warehouseRepository.DeleteAsync(warehouse);
    }
}
