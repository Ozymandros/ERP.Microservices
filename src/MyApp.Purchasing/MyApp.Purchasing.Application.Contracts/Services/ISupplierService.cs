using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Contracts.Services;

public interface ISupplierService
{
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id);
    Task<SupplierDto?> GetSupplierByEmailAsync(string email);
    Task<IEnumerable<SupplierDto>> GetSuppliersByNameAsync(string name);
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
    Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec);
    Task<SupplierDto> CreateSupplierAsync(CreateUpdateSupplierDto dto);
    Task<SupplierDto> UpdateSupplierAsync(Guid id, CreateUpdateSupplierDto dto);
    Task DeleteSupplierAsync(Guid id);
}
