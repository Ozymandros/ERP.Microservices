using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Services;

public class SupplierService : AppServiceBase, ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the SupplierService class.
    /// </summary>
    /// <param name="supplierRepository">The supplier Repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public SupplierService(
        ISupplierRepository supplierRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<SupplierService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Purchasing)
    {
        _supplierRepository = supplierRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the supplier by id asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<SupplierDto?> GetSupplierByIdAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
    }

    /// <summary>
    /// Gets the supplier by email asynchronously.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<SupplierDto?> GetSupplierByEmailAsync(string email)
    {
        var supplier = await _supplierRepository.GetByEmailAsync(email);
        return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
    }

    /// <summary>
    /// Gets the supplier by name asynchronously.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<SupplierDto?> GetSupplierByNameAsync(string name)
    {
        var suppliers = await _supplierRepository.GetByNameAsync(name);
        var supplier = suppliers.FirstOrDefault();
        return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
    }

    /// <summary>
    /// Gets the suppliers by name asynchronously.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<SupplierDto>> GetSuppliersByNameAsync(string name)
    {
        var suppliers = await _supplierRepository.GetByNameAsync(name);
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    /// <summary>
    /// Gets all suppliers asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    /// <summary>
    /// Creates a supplier asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
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
        await SaveChangesAsync();

        return _mapper.Map<SupplierDto>(createdSupplier);
    }

    /// <summary>
    /// Updates the supplier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
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
        await SaveChangesAsync();

        return _mapper.Map<SupplierDto>(updatedSupplier);
    }

    /// <summary>
    /// Deletes the supplier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    public async Task DeleteSupplierAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{id}' not found.");
        }

        await _supplierRepository.DeleteAsync(supplier);
        await SaveChangesAsync();
    }

    /// <summary>
    /// Query suppliers asynchronously.
    /// </summary>
    /// <param name="spec">The spec.</param>
    public async Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec)
    {
        var result = await _supplierRepository.QueryAsync(spec);
        var dtos = result.Items.Select(s => _mapper.Map<SupplierDto>(s)).ToList();
        return new PaginatedResult<SupplierDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
