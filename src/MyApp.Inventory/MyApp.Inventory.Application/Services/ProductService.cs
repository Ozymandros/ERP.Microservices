using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

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

    public async Task<PaginatedResult<ProductDto>> GetAllProductsPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedProducts = await _productRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(paginatedProducts.Items);
        return new PaginatedResult<ProductDto>(productDtos, paginatedProducts.PageNumber, paginatedProducts.PageSize, paginatedProducts.TotalCount);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await _productRepository.GetLowStockProductsAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> CreateProductAsync(CreateUpdateProductDto dto)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"A\",\"location\":\"ProductService.cs:53\",\"message\":\"CreateProductAsync entry\",\"data\":{{\"dtoSKU\":\"{dto.SKU}\",\"dtoName\":\"{dto.Name}\",\"dtoHasId\":false}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
        // #endregion

        // Check if product with same SKU already exists
        var existingProduct = await _productRepository.GetBySkuAsync(dto.SKU);
        if (existingProduct != null)
        {
            throw new InvalidOperationException($"Product with SKU '{dto.SKU}' already exists.");
        }

        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"B\",\"location\":\"ProductService.cs:62\",\"message\":\"Before AutoMapper.Map\",\"data\":{{\"dtoType\":\"{dto.GetType().FullName}\",\"targetType\":\"Product\",\"mapperType\":\"{_mapper.GetType().FullName}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
        // #endregion

        Product? product = null;
        try
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"B\",\"location\":\"ProductService.cs:70\",\"message\":\"Attempting AutoMapper.Map\",\"data\":{{}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
            // #endregion

            product = _mapper.Map<Product>(dto);

            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"B\",\"location\":\"ProductService.cs:75\",\"message\":\"AutoMapper.Map succeeded\",\"data\":{{\"productCreated\":{product != null},\"productId\":\"{product?.Id}\",\"productSKU\":\"{product?.SKU}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"B\",\"location\":\"ProductService.cs:79\",\"message\":\"AutoMapper.Map failed\",\"data\":{{\"exceptionType\":\"{ex.GetType().FullName}\",\"exceptionMessage\":\"{ex.Message.Replace("\"", "\\\"")}\",\"stackTrace\":\"{ex.StackTrace?.Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
            // #endregion
            throw;
        }

        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"A\",\"location\":\"ProductService.cs:64\",\"message\":\"After AutoMapper.Map\",\"data\":{{\"productCreated\":{product != null},\"productId\":\"{product?.Id}\",\"productSKU\":\"{product?.SKU}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
        // #endregion

        var createdProduct = await _productRepository.AddAsync(product);

        // #region agent log
        try { System.IO.File.AppendAllText(@"c:\Projects\ERP_ASPIRE_APP\erp-backend\.cursor\debug.log", $"{{\"sessionId\":\"debug-session\",\"runId\":\"post-fix\",\"hypothesisId\":\"A\",\"location\":\"ProductService.cs:66\",\"message\":\"Product created successfully\",\"data\":{{\"createdProductId\":\"{createdProduct?.Id}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n"); } catch { }
        // #endregion

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

    /// <summary>
    /// Query products with filtering, sorting, and pagination
    /// </summary>
    public async Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec)
    {
        var result = await _productRepository.QueryAsync(spec);
        var dtos = result.Items.Select(p => _mapper.Map<ProductDto>(p)).ToList();
        return new PaginatedResult<ProductDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }
}
