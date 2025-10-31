using Microsoft.AspNetCore.Mvc;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;

namespace MyApp.Purchasing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/purchasing/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ISupplierService supplierService, ICacheService cacheService, ILogger<SuppliersController> logger)
    {
        _supplierService = supplierService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get all suppliers - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAllSuppliers()
    {
        try
        {
            var suppliers = await _cacheService.GetStateAsync<IEnumerable<SupplierDto>>("all_suppliers");
            if (suppliers != null)
            {
                return Ok(suppliers);
            }

            suppliers = await _supplierService.GetAllSuppliersAsync();
            await _cacheService.SaveStateAsync("all_suppliers", suppliers);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all suppliers");
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return Ok(suppliers);
        }
    }

    /// <summary>
    /// Get supplier by ID - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> GetSupplierById(Guid id)
    {
        try
        {
            string cacheKey = $"Supplier-{id}";
            var supplier = await _cacheService.GetStateAsync<SupplierDto>(cacheKey);

            if (supplier != null)
            {
                return Ok(supplier);
            }

            supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier with ID {SupplierId} not found", id);
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, supplier);
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            return supplier == null ? NotFound() : Ok(supplier);
        }
    }

    /// <summary>
    /// Get supplier by email - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("email/{email}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> GetSupplierByEmail(string email)
    {
        _logger.LogInformation("Retrieving supplier with email: {Email}", email);
        var supplier = await _supplierService.GetSupplierByEmailAsync(email);
        if (supplier == null)
        {
            _logger.LogWarning("Supplier with email {Email} not found", email);
            return NotFound();
        }
        return Ok(supplier);
    }

    /// <summary>
    /// Search suppliers by name - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("search/{name}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> SearchSuppliersByName(string name)
    {
        _logger.LogInformation("Searching suppliers with name: {Name}", name);
        var suppliers = await _supplierService.GetSuppliersByNameAsync(name);
        return Ok(suppliers);
    }

    /// <summary>
    /// Create a new supplier - Requires Purchasing.Create permission
    /// </summary>
    [HttpPost]
    [HasPermission("Purchasing", "Create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateUpdateSupplierDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Creating new supplier: {Name}", dto.Name);
            var supplier = await _supplierService.CreateSupplierAsync(dto);
            await _cacheService.RemoveStateAsync("all_suppliers");
            return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict creating supplier: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing supplier - Requires Purchasing.Update permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Purchasing", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(Guid id, [FromBody] CreateUpdateSupplierDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Updating supplier with ID: {SupplierId}", id);
            var supplier = await _supplierService.UpdateSupplierAsync(id, dto);
            string cacheKey = $"Supplier-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_suppliers");
            return Ok(supplier);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict updating supplier: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Delete a supplier - Requires Purchasing.Delete permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("Purchasing", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting supplier with ID: {SupplierId}", id);
            await _supplierService.DeleteSupplierAsync(id);
            string cacheKey = $"Supplier-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_suppliers");
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }
}
