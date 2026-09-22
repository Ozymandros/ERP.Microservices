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

/// <summary>Provides product management operations for the Inventory service.</summary>
public class ProductService : AppServiceBase, IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="ProductService"/>.</summary>
    /// Initializes a new instance of the ProductService class.
    /// <param name="productRepository">The product Repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public ProductService(
        IProductRepository productRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ProductService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Inventory)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    /// <summary>Retrieves a product by its unique identifier.</summary>
    /// Gets the product by id asynchronously.
    /// <param name="id">The id.</param>
    /// <returns>The matching <see cref="ProductDto"/>, or <c>null</c> if not found.</returns>
    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    /// <summary>Retrieves a product by its SKU.</summary>
    /// Gets the product by sku asynchronously.
    /// <param name="sku">The sku.</param>
    /// <returns>The matching <see cref="ProductDto"/>, or <c>null</c> if not found.</returns>
    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        var product = await _productRepository.GetBySkuAsync(sku);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    /// <summary>Retrieves a product by its name.</summary>
    /// Gets the product by name asynchronously.
    /// <param name="name">The name.</param>
    /// <returns>The matching <see cref="ProductDto"/>, or <c>null</c> if not found.</returns>
    public async Task<ProductDto?> GetProductByNameAsync(string name)
    {
        var product = await _productRepository.GetByNameAsync(name);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    /// <summary>Retrieves all products in the inventory.</summary>
    /// Gets all products asynchronously.
    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    /// <summary>Retrieves a paginated list of all products.</summary>
    /// Gets all products paginated asynchronously.
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <returns>A <see cref="PaginatedResult{ProductDto}"/> for the requested page.</returns>
    public async Task<PaginatedResult<ProductDto>> GetAllProductsPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedProducts = await _productRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(paginatedProducts.Items);
        return new PaginatedResult<ProductDto>(productDtos, paginatedProducts.PageNumber, paginatedProducts.PageSize, paginatedProducts.TotalCount);
    }

    /// <summary>Retrieves all products whose stock level is below their configured reorder level.</summary>
    /// Gets the low stock products asynchronously.
    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await _productRepository.GetLowStockProductsAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    /// <summary>Creates a new product from the supplied data.</summary>
    /// Creates a product asynchronously.
    /// <param name="dto">The dto.</param>
    /// <returns>The newly created <see cref="ProductDto"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a product with the same SKU already exists.</exception>
    public async Task<ProductDto> CreateProductAsync(CreateUpdateProductDto dto)
    {
        // Check if product with same SKU already exists
        var existingProduct = await _productRepository.GetBySkuAsync(dto.SKU);
        if (existingProduct != null)
        {
            throw new InvalidOperationException($"Product with SKU '{dto.SKU}' already exists.");
        }

        var product = _mapper.Map<Product>(dto);
        var createdProduct = await _productRepository.AddAsync(product);
        await SaveChangesAsync();

        return _mapper.Map<ProductDto>(createdProduct);
    }

    /// <summary>Updates an existing product with the supplied data.</summary>
    /// Updates the product asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <returns>The updated <see cref="ProductDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no product with the given identifier is found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the new SKU is already in use by another product.</exception>
    public async Task<ProductDto> UpdateProductAsync(Guid id, CreateUpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{id}' not found.");
        }

        // Check if new SKU is already used by another product
        if (product.SKU != dto.SKU)
        {
            var existingProduct = await _productRepository.GetBySkuAsync(dto.SKU);
            if (existingProduct != null)
            {
                throw new InvalidOperationException($"Product with SKU '{dto.SKU}' already exists.");
            }
        }

        _mapper.Map(dto, product);
        var updatedProduct = await _productRepository.UpdateAsync(product);
        await SaveChangesAsync();

        return _mapper.Map<ProductDto>(updatedProduct);
    }

    /// <summary>Deletes the product with the specified identifier.</summary>
    /// Deletes the product asynchronously.
    /// <param name="id">The id.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no product with the given identifier is found.</exception>
    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{id}' not found.");
        }

        await _productRepository.DeleteAsync(product);
        await SaveChangesAsync();
    }

    /// <summary>Queries products using a specification that encapsulates filtering, sorting and pagination.</summary>
    /// Query products asynchronously.
    /// <param name="spec">The spec.</param>
    /// <returns>A <see cref="PaginatedResult{ProductDto}"/> matching the specification.</returns>
    public async Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec)
    {
        var result = await _productRepository.QueryAsync(spec);
        var dtos = result.Items.Select(p => _mapper.Map<ProductDto>(p)).ToList();
        return new PaginatedResult<ProductDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
