using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;

namespace MyApp.Inventory.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
    {
        _logger.LogInformation("Retrieving all products");
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return NotFound();
        }
        return Ok(product);
    }

    /// <summary>
    /// Get product by SKU
    /// </summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductBySku(string sku)
    {
        _logger.LogInformation("Retrieving product with SKU: {SKU}", sku);
        var product = await _productService.GetProductBySkuAsync(sku);
        if (product == null)
        {
            _logger.LogWarning("Product with SKU {SKU} not found", sku);
            return NotFound();
        }
        return Ok(product);
    }

    /// <summary>
    /// Get low stock products
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts()
    {
        _logger.LogInformation("Retrieving low stock products");
        var products = await _productService.GetLowStockProductsAsync();
        return Ok(products);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
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
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict creating product: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id}")]
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
    /// Delete a product
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);
            await _productService.DeleteProductAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Product not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }
}
