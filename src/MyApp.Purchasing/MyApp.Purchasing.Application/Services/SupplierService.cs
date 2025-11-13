using AutoMapper;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IMapper _mapper;

    public SupplierService(ISupplierRepository supplierRepository, IMapper mapper)
    {
        _supplierRepository = supplierRepository;
        _mapper = mapper;
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<SupplierDto?> GetSupplierByEmailAsync(string email)
    {
        var supplier = await _supplierRepository.GetByEmailAsync(email);
        return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<IEnumerable<SupplierDto>> GetSuppliersByNameAsync(string name)
    {
        var suppliers = await _supplierRepository.GetByNameAsync(name);
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateUpdateSupplierDto dto)
    {
        // Check if supplier with same email already exists
        var existingSupplier = await _supplierRepository.GetByEmailAsync(dto.Email);
        if (existingSupplier != null)
        {
            throw new InvalidOperationException($"Supplier with email '{dto.Email}' already exists.");
        }

        var supplier = _mapper.Map<Supplier>(dto);
        var createdSupplier = await _supplierRepository.AddAsync(supplier);

        return _mapper.Map<SupplierDto>(createdSupplier);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(Guid id, CreateUpdateSupplierDto dto)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{id}' not found.");
        }

        // Check if new email is already used by another supplier
        if (supplier.Email != dto.Email)
        {
            var existingSupplier = await _supplierRepository.GetByEmailAsync(dto.Email);
            if (existingSupplier != null)
            {
                throw new InvalidOperationException($"Supplier with email '{dto.Email}' already exists.");
            }
        }

        _mapper.Map(dto, supplier);
        var updatedSupplier = await _supplierRepository.UpdateAsync(supplier);

        return _mapper.Map<SupplierDto>(updatedSupplier);
    }

    public async Task DeleteSupplierAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{id}' not found.");
        }

        await _supplierRepository.DeleteAsync(supplier);
    }

    /// <summary>
    /// Query suppliers with filtering, sorting, and pagination
    /// </summary>
    public async Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec)
    {
        var result = await _supplierRepository.QueryAsync(spec);
        var dtos = result.Items.Select(s => _mapper.Map<SupplierDto>(s)).ToList();
        return new PaginatedResult<SupplierDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
