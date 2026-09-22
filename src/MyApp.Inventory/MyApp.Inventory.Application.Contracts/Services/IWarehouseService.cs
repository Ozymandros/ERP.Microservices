using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Warehouse Service.
/// </summary>
public interface IWarehouseService
{
    /// <summary>Retrieves a warehouse by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the warehouse.</param>
    /// <returns>The matching <see cref="WarehouseDto"/>, or <c>null</c> if not found.</returns>
    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);

    /// <summary>Retrieves a warehouse by its name.</summary>
    /// <param name="name">The warehouse name to search for.</param>
    /// <returns>The matching <see cref="WarehouseDto"/>, or <c>null</c> if not found.</returns>
    Task<WarehouseDto?> GetWarehouseByNameAsync(string name);

    /// <summary>Retrieves all warehouses.</summary>
    /// <returns>A collection of all <see cref="WarehouseDto"/> records.</returns>
    Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();

    /// <summary>Retrieves a paginated list of all warehouses.</summary>
    /// <param name="pageNumber">The one-based page number to return.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PaginatedResult{WarehouseDto}"/> for the requested page.</returns>
    Task<PaginatedResult<WarehouseDto>> GetAllWarehousesPaginatedAsync(int pageNumber, int pageSize);

    /// <summary>Queries warehouses using a specification that encapsulates filtering, sorting and pagination.</summary>
    /// <param name="spec">The specification to apply.</param>
    /// <returns>A <see cref="PaginatedResult{WarehouseDto}"/> matching the specification.</returns>
    Task<PaginatedResult<WarehouseDto>> QueryWarehousesAsync(ISpecification<Warehouse> spec);

    /// <summary>Creates a new warehouse from the supplied data.</summary>
    /// <param name="dto">The data used to create the warehouse.</param>
    /// <returns>The newly created <see cref="WarehouseDto"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a warehouse with the same name already exists.</exception>
    Task<WarehouseDto> CreateWarehouseAsync(CreateUpdateWarehouseDto dto);

    /// <summary>Updates an existing warehouse with the supplied data.</summary>
    /// <param name="id">The unique identifier of the warehouse to update.</param>
    /// <param name="dto">The updated warehouse data.</param>
    /// <returns>The updated <see cref="WarehouseDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no warehouse with the given identifier is found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the new name is already in use by another warehouse.</exception>
    Task<WarehouseDto> UpdateWarehouseAsync(Guid id, CreateUpdateWarehouseDto dto);

    /// <summary>Deletes the warehouse with the specified identifier.</summary>
    /// <param name="id">The unique identifier of the warehouse to delete.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no warehouse with the given identifier is found.</exception>
    Task DeleteWarehouseAsync(Guid id);
}
