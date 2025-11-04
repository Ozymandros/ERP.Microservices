using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;

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
            _logger.LogInformation("Retrieved paginated products: Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated products");
            return StatusCode(500, new { message = "An error occurred retrieving products" });
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
            string cacheKey = $"Product-{id}";
            var product = await _cacheService.GetStateAsync<ProductDto>(cacheKey);

            if (product != null)
            {
                _logger.LogInformation("Retrieved product {ProductId} from cache", id);
                return Ok(product);
            }

            product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", id);
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, product);
            _logger.LogInformation("Retrieved product {ProductId} from database and cached", id);
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
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
            string cacheKey = $"Product-SKU-{sku}";
            var product = await _cacheService.GetStateAsync<ProductDto>(cacheKey);

            if (product != null)
            {
                _logger.LogInformation("Retrieved product with SKU {SKU} from cache", sku);
                return Ok(product);
            }

            product = await _productService.GetProductBySkuAsync(sku);
            if (product == null)
            {
                _logger.LogWarning("Product with SKU {SKU} not found", sku);
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, product);
            _logger.LogInformation("Retrieved product with SKU {SKU} from database and cached", sku);
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with SKU {SKU}", sku);
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
            _logger.LogInformation("Creating new product with SKU: {SKU}", dto.SKU);
            var product = await _productService.CreateProductAsync(dto);
            await _cacheService.RemoveStateAsync("all_products");
            await _cacheService.RemoveStateAsync("low_stock_products");
            _logger.LogInformation("Product created and cache invalidated");
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict creating product: {Message}", ex.Message);
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
            _logger.LogInformation("Updating product with ID: {ProductId}", id);
            var product = await _productService.UpdateProductAsync(id, dto);
            string cacheKey = $"Product-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_products");
            await _cacheService.RemoveStateAsync("low_stock_products");
            _logger.LogInformation("Product {ProductId} updated and cache invalidated", id);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Product not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict updating product: {Message}", ex.Message);
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
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);
            await _productService.DeleteProductAsync(id);
            string cacheKey = $"Product-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_products");
            await _cacheService.RemoveStateAsync("low_stock_products");
            _logger.LogInformation("Product {ProductId} deleted and cache invalidated", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Product not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }
}
