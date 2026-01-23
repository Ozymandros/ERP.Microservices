using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;


using MyApp.Shared.Infrastructure.Export;
namespace MyApp.Inventory.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger, ICacheService cacheService)
    {
        _productService = productService;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Export all products as XLSX
    /// </summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Inventory", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var products = await _cacheService.GetStateAsync<IEnumerable<ProductDto>>("all_products")
                ?? await _productService.GetAllProductsAsync();
            var bytes = products.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting products to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting products" });
        }
    }

    /// <summary>
    /// Get all products - Requires Inventory.Read permission
    /// </summary>
    [HttpGet]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
    {
        try
        {
            var products = await _cacheService.GetStateAsync<IEnumerable<ProductDto>>("all_products");
            if (products != null)
            {
                _logger.LogInformation("Retrieved all products from cache");
                return Ok(products);
            }

            products = await _productService.GetAllProductsAsync();
            await _cacheService.SaveStateAsync("all_products", products);
            _logger.LogInformation("Retrieved all products from database and cached");
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }
    }

    /// <summary>
    /// Get all products with pagination - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("paginated")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> GetAllProductsPaginated([FromQuery(Name = "page")] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _productService.GetAllProductsPaginatedAsync(pageNumber, pageSize);
            _logger.LogInformation("Retrieved paginated products: {@Pagination}", new { PageNumber = pageNumber, PageSize = pageSize });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated products");
            return StatusCode(500, new { message = "An error occurred retrieving products" });
        }
    }

    /// <summary>
    /// Search products with advanced filtering, sorting, and pagination - Requires Inventory.Read permission
    /// </summary>
    /// <remarks>
    /// Supported filters: sku, name, category, isActive, minPrice, maxPrice
    /// Supported sort fields: id, sku, name, unitPrice, stock, createdAt
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            query.Validate();
            var spec = new ProductQuerySpec(query);
            var result = await _productService.QueryProductsAsync(spec);
            _logger.LogInformation("Searched products with query: {@Query}", query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { message = "An error occurred searching products" });
        }
    }

    /// <summary>
    /// Get product by ID - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        try
        {
            string cacheKey = "Product-" + id;
            var product = await _cacheService.GetStateAsync<ProductDto>(cacheKey);

            if (product != null)
            {
                _logger.LogInformation("Retrieved product {@Product} from cache", new { ProductId = id });
                return Ok(product);
            }

            product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {@Product} not found", new { ProductId = id });
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, product);
            _logger.LogInformation("Retrieved product {@Product} from database and cached", new { ProductId = id });
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {@Product}", new { ProductId = id });
            var product = await _productService.GetProductByIdAsync(id);
            return product == null ? NotFound() : Ok(product);
        }
    }

    /// <summary>
    /// Get product by SKU - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("sku/{sku}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductBySku(string sku)
    {
        try
        {
            string cacheKey = "Product-SKU-" + sku;
            var product = await _cacheService.GetStateAsync<ProductDto>(cacheKey);

            if (product != null)
            {
                _logger.LogInformation("Retrieved product with SKU {@Product} from cache", new { SKU = sku });
                return Ok(product);
            }

            product = await _productService.GetProductBySkuAsync(sku);
            if (product == null)
            {
                _logger.LogWarning("Product with SKU {@Product} not found", new { SKU = sku });
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, product);
            _logger.LogInformation("Retrieved product with SKU {@Product} from database and cached", new { SKU = sku });
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with SKU {@Product}", new { SKU = sku });
            var product = await _productService.GetProductBySkuAsync(sku);
            return product == null ? NotFound() : Ok(product);
        }
    }

    /// <summary>
    /// Get low stock products - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("low-stock")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts()
    {
        try
        {
            var products = await _cacheService.GetStateAsync<IEnumerable<ProductDto>>("low_stock_products");
            if (products != null)
            {
                _logger.LogInformation("Retrieved low stock products from cache");
                return Ok(products);
            }

            products = await _productService.GetLowStockProductsAsync();
            await _cacheService.SaveStateAsync("low_stock_products", products, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Retrieved low stock products from database and cached with 5-minute TTL");
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            var products = await _productService.GetLowStockProductsAsync();
            return Ok(products);
        }
    }

    /// <summary>
    /// Create a new product - Requires Inventory.Create permission
    /// </summary>
    [HttpPost]
    [HasPermission("Inventory", "Create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateUpdateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Creating new product with SKU: {@Product}", new { SKU = dto.SKU });
            var product = await _productService.CreateProductAsync(dto);
            await _cacheService.RemoveStateAsync("all_products");
            await _cacheService.RemoveStateAsync("low_stock_products");
            _logger.LogInformation("Product {@Product} created and cache invalidated", new { ProductId = product.Id });
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict creating product: {@Error}", new { Message = ex.Message });
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update a product - Requires Inventory.Update permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Inventory", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] CreateUpdateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Updating product with ID: {@Product}", new { ProductId = id });
            var product = await _productService.UpdateProductAsync(id, dto);
            string cacheKey = "Product-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_products");
            await _cacheService.RemoveStateAsync("low_stock_products");
            _logger.LogInformation("Product {@Product} updated and cache invalidated", new { ProductId = id });
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict updating product: {@Error}", new { Message = ex.Message });
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Delete a product - Requires Inventory.Delete permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("Inventory", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting product with ID: {@Product}", new { ProductId = id });
            await _productService.DeleteProductAsync(id);
            string cacheKey = "Product-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_products");
            await _cacheService.RemoveStateAsync("low_stock_products");
            _logger.LogInformation("Product {@Product} deleted and cache invalidated", new { ProductId = id });
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }
}
