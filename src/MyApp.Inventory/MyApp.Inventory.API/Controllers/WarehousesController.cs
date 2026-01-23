using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Inventory.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;

using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Inventory.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    private readonly ILogger<WarehousesController> _logger;


    /// <summary>
    /// Export all warehouses as XLSX
    /// </summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Inventory", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            var bytes = warehouses.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Warehouses.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting warehouses to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting warehouses" });
        }
    }

    /// <summary>
    /// Export all warehouses as PDF
    /// </summary>
    [HttpGet("export-pdf")]
    [HasPermission("Inventory", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            var bytes = warehouses.ExportToPdf();
            return File(bytes, "application/pdf", "Warehouses.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting warehouses to PDF");
            return StatusCode(500, new { message = "An error occurred exporting warehouses" });
        }
    }

    public WarehousesController(IWarehouseService warehouseService, ILogger<WarehousesController> logger)
    {
        _warehouseService = warehouseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all warehouses - Requires Inventory.Read permission
    /// </summary>
    [HttpGet]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetAllWarehouses()
    {
        _logger.LogInformation("Retrieving all warehouses");
        var warehouses = await _warehouseService.GetAllWarehousesAsync();
        return Ok(warehouses);
    }

    /// <summary>
    /// Get all warehouses with pagination - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("paginated")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<WarehouseDto>>> GetAllWarehousesPaginated([FromQuery(Name = "page")] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving paginated warehouses: {@Pagination}", new { PageNumber = pageNumber, PageSize = pageSize });
            var result = await _warehouseService.GetAllWarehousesPaginatedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated warehouses");
            return StatusCode(500, new { message = "An error occurred retrieving warehouses" });
        }
    }

    /// <summary>
    /// Search warehouses with advanced filtering, sorting, and pagination - Requires Inventory.Read permission
    /// </summary>
    /// <remarks>
    /// Supported filters: name, location, city, country, isActive
    /// Supported sort fields: id, name, location, city, country, createdAt
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<WarehouseDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            query.Validate();
            var spec = new WarehouseQuerySpec(query);
            var result = await _warehouseService.QueryWarehousesAsync(spec);
            _logger.LogInformation("Searched warehouses with query: {@Query}", query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching warehouses");
            return StatusCode(500, new { message = "An error occurred searching warehouses" });
        }
    }

    /// <summary>
    /// Get warehouse by ID - Requires Inventory.Read permission
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Inventory", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDto>> GetWarehouseById(Guid id)
    {
        _logger.LogInformation("Retrieving warehouse with ID: {@Warehouse}", new { WarehouseId = id });
        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
        if (warehouse == null)
        {
            _logger.LogWarning("Warehouse with ID {@Warehouse} not found", new { WarehouseId = id });
            return NotFound();
        }
        return Ok(warehouse);
    }

    /// <summary>
    /// Create a new warehouse - Requires Inventory.Create permission
    /// </summary>
    [HttpPost]
    [HasPermission("Inventory", "Create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateUpdateWarehouseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Creating new warehouse: {@Warehouse}", new { Name = dto.Name });
            var warehouse = await _warehouseService.CreateWarehouseAsync(dto);
            return CreatedAtAction(nameof(GetWarehouseById), new { id = warehouse.Id }, warehouse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict creating warehouse: {@Error}", new { Message = ex.Message });
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing warehouse - Requires Inventory.Update permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Inventory", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(Guid id, [FromBody] CreateUpdateWarehouseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Updating warehouse with ID: {@Warehouse}", new { WarehouseId = id });
            var warehouse = await _warehouseService.UpdateWarehouseAsync(id, dto);
            return Ok(warehouse);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Warehouse not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict updating warehouse: {@Error}", new { Message = ex.Message });
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Delete a warehouse - Requires Inventory.Delete permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("Inventory", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWarehouse(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting warehouse with ID: {@Warehouse}", new { WarehouseId = id });
            await _warehouseService.DeleteWarehouseAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Warehouse not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }
}
