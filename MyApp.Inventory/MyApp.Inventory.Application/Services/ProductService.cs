using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        var product = await _productRepository.GetBySkuAsync(sku);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await _productRepository.GetLowStockProductsAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

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

        return _mapper.Map<ProductDto>(createdProduct);
    }

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

        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{id}' not found.");
        }

        await _productRepository.DeleteAsync(product);
    }
}
