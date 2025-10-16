using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IWarehouseService
{
    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);
    Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
    Task<WarehouseDto> CreateWarehouseAsync(CreateUpdateWarehouseDto dto);
    Task<WarehouseDto> UpdateWarehouseAsync(Guid id, CreateUpdateWarehouseDto dto);
    Task DeleteWarehouseAsync(Guid id);
}
