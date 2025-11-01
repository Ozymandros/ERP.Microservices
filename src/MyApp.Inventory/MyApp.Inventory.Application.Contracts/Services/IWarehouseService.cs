using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IWarehouseService
{
    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);
    Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
    Task<PaginatedResult<WarehouseDto>> GetAllWarehousesPaginatedAsync(int pageNumber, int pageSize);
    Task<WarehouseDto> CreateWarehouseAsync(CreateUpdateWarehouseDto dto);
    Task<WarehouseDto> UpdateWarehouseAsync(Guid id, CreateUpdateWarehouseDto dto);
    Task DeleteWarehouseAsync(Guid id);
}
