using Microsoft.AspNetCore.Mvc;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;

namespace MyApp.Purchasing.API.Controllers;

[ApiController]
[Route("api/purchasing/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger)
    {
        _supplierService = supplierService;
        _logger = logger;
    }

    /// <summary>
    /// Get all suppliers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAllSuppliers()
    {
        _logger.LogInformation("Retrieving all suppliers");
        var suppliers = await _supplierService.GetAllSuppliersAsync();
        return Ok(suppliers);
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> GetSupplierById(Guid id)
    {
        _logger.LogInformation("Retrieving supplier with ID: {SupplierId}", id);
        var supplier = await _supplierService.GetSupplierByIdAsync(id);
        if (supplier == null)
        {
            _logger.LogWarning("Supplier with ID {SupplierId} not found", id);
            return NotFound();
        }
        return Ok(supplier);
    }

    /// <summary>
    /// Get supplier by email
    /// </summary>
    [HttpGet("email/{email}")]
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
    /// Search suppliers by name
    /// </summary>
    [HttpGet("search/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> SearchSuppliersByName(string name)
    {
        _logger.LogInformation("Searching suppliers with name: {Name}", name);
        var suppliers = await _supplierService.GetSuppliersByNameAsync(name);
        return Ok(suppliers);
    }

    /// <summary>
    /// Create a new supplier
    /// </summary>
    [HttpPost]
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
            return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict creating supplier: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    [HttpPut("{id}")]
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
    /// Delete a supplier
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting supplier with ID: {SupplierId}", id);
            await _supplierService.DeleteSupplierAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Supplier not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }
}
