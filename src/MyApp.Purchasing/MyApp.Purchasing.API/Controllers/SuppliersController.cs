using Microsoft.AspNetCore.Mvc;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;


using MyApp.Shared.Infrastructure.Export;
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
    /// Export all suppliers as XLSX
    /// </summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Purchasing", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var suppliers = await _cacheService.GetStateAsync<IEnumerable<SupplierDto>>("all_suppliers")
                ?? await _supplierService.GetAllSuppliersAsync();
            var bytes = suppliers.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Suppliers.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting suppliers to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting suppliers" });
        }
    }

    /// <summary>
    /// Export all suppliers as PDF
    /// </summary>
    [HttpGet("export-pdf")]
    [HasPermission("Purchasing", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var suppliers = await _cacheService.GetStateAsync<IEnumerable<SupplierDto>>("all_suppliers")
                ?? await _supplierService.GetAllSuppliersAsync();
            var bytes = suppliers.ExportToPdf();
            return File(bytes, "application/pdf", "Suppliers.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting suppliers to PDF");
            return StatusCode(500, new { message = "An error occurred exporting suppliers" });
        }
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
            string cacheKey = "Supplier-" + id;
            var supplier = await _cacheService.GetStateAsync<SupplierDto>(cacheKey);

            if (supplier != null)
            {
                return Ok(supplier);
            }

            supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier with ID {@Supplier} not found", new { SupplierId = id });
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, supplier);
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier {@Supplier}", new { SupplierId = id });
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
        _logger.LogInformation("Retrieving supplier with email: {@Email}", new { Email = email });
        var supplier = await _supplierService.GetSupplierByEmailAsync(email);
        if (supplier == null)
        {
            _logger.LogWarning("Supplier with email {@Email} not found", new { Email = email });
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
        _logger.LogInformation("Searching suppliers with name: {@Name}", new { Name = name });
        var suppliers = await _supplierService.GetSuppliersByNameAsync(name);
        return Ok(suppliers);
    }

    /// <summary>
    /// Search suppliers with advanced filtering, sorting, and pagination - Requires Purchasing.Read permission
    /// </summary>
    /// <remarks>
    /// Supported filters: name, email, country, city, isActive
    /// Supported sort fields: id, name, email, city, country, createdAt
    /// </remarks>
    [HttpGet("advanced-search")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<SupplierDto>>> AdvancedSearch([FromQuery] QuerySpec query)
    {
        try
        {
            query.Validate();
            var spec = new SupplierQuerySpec(query);
            var result = await _supplierService.QuerySuppliersAsync(spec);
            _logger.LogInformation("Searched suppliers with query: {@Query}", query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching suppliers");
            return StatusCode(500, new { message = "An error occurred searching suppliers" });
        }
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
            _logger.LogInformation("Creating new supplier: {@Supplier}", new { Name = dto.Name });
            var supplier = await _supplierService.CreateSupplierAsync(dto);
            await _cacheService.RemoveStateAsync("all_suppliers");
            return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict creating supplier: {@Error}", new { Message = ex.Message });
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
            _logger.LogInformation("Updating supplier with ID: {@Supplier}", new { SupplierId = id });
            var supplier = await _supplierService.UpdateSupplierAsync(id, dto);
            string cacheKey = "Supplier-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_suppliers");
            return Ok(supplier);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Supplier not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict updating supplier: {@Error}", new { Message = ex.Message });
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
            _logger.LogInformation("Deleting supplier with ID: {@Supplier}", new { SupplierId = id });
            await _supplierService.DeleteSupplierAsync(id);
            string cacheKey = "Supplier-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_suppliers");
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Supplier not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }
}
