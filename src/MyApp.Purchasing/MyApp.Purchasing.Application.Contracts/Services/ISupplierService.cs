using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Supplier Service.
/// </summary>
public interface ISupplierService
{
    /// <summary>Retrieves a supplier by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the supplier.</param>
    /// <returns>The supplier DTO, or <c>null</c> if not found.</returns>
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id);
    /// <summary>Retrieves a supplier by email address.</summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The supplier DTO, or <c>null</c> if not found.</returns>
    Task<SupplierDto?> GetSupplierByEmailAsync(string email);
    /// <summary>Retrieves the first supplier whose name matches the specified value.</summary>
    /// <param name="name">The name to look up.</param>
    /// <returns>The matching supplier DTO, or <c>null</c> if not found.</returns>
    Task<SupplierDto?> GetSupplierByNameAsync(string name);
    /// <summary>Retrieves all suppliers whose name contains the specified search term.</summary>
    /// <param name="name">The name fragment to search for.</param>
    /// <returns>A collection of matching supplier DTOs.</returns>
    Task<IEnumerable<SupplierDto>> GetSuppliersByNameAsync(string name);
    /// <summary>Retrieves all suppliers.</summary>
    /// <returns>A collection of all supplier DTOs.</returns>
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
    /// <summary>Retrieves a paginated, filtered and sorted collection of suppliers.</summary>
    /// <param name="spec">The specification that defines filtering, sorting, and paging.</param>
    /// <returns>A paginated result containing matching supplier DTOs.</returns>
    Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec);
    /// <summary>Creates a new supplier.</summary>
    /// <param name="dto">The data for the new supplier.</param>
    /// <returns>The created supplier DTO.</returns>
    Task<SupplierDto> CreateSupplierAsync(CreateUpdateSupplierDto dto);
    /// <summary>Updates an existing supplier.</summary>
    /// <param name="id">The unique identifier of the supplier to update.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>The updated supplier DTO.</returns>
    Task<SupplierDto> UpdateSupplierAsync(Guid id, CreateUpdateSupplierDto dto);
    /// <summary>Deletes the supplier with the specified identifier.</summary>
    /// <param name="id">The unique identifier of the supplier to delete.</param>
    Task DeleteSupplierAsync(Guid id);
}
