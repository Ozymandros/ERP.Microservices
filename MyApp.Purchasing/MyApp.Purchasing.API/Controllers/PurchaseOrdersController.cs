using Microsoft.AspNetCore.Mvc;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace MyApp.Purchasing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/purchasing/orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService, ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all purchase orders - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetAllPurchaseOrders()
    {
        _logger.LogInformation("Retrieving all purchase orders");
        var orders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Get purchase order by ID - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrderById(Guid id)
    {
        _logger.LogInformation("Retrieving purchase order with ID: {OrderId}", id);
        var order = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Purchase order with ID {OrderId} not found", id);
            return NotFound();
        }
        return Ok(order);
    }

    /// <summary>
    /// Get purchase orders by supplier - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("supplier/{supplierId}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrdersBySupplier(Guid supplierId)
    {
        _logger.LogInformation("Retrieving purchase orders for supplier: {SupplierId}", supplierId);
        var orders = await _purchaseOrderService.GetPurchaseOrdersBySupplierAsync(supplierId);
        return Ok(orders);
    }

    /// <summary>
    /// Get purchase orders by status - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("status/{status}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrdersByStatus(string status)
    {
        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var orderStatus))
        {
            _logger.LogWarning("Invalid order status: {Status}", status);
            return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames(typeof(PurchaseOrderStatus)))}");
        }

        _logger.LogInformation("Retrieving purchase orders with status: {Status}", status);
        var orders = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(orderStatus);
        return Ok(orders);
    }

    /// <summary>
    /// Create a new purchase order - Requires Purchasing.Write permission
    /// </summary>
    [HttpPost]
    [HasPermission("Purchasing", "Write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreateUpdatePurchaseOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Creating new purchase order: {OrderNumber}", dto.OrderNumber);
            var order = await _purchaseOrderService.CreatePurchaseOrderAsync(dto);
            return CreatedAtAction(nameof(GetPurchaseOrderById), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing purchase order - Requires Purchasing.Write permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Purchasing", "Write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> UpdatePurchaseOrder(Guid id, [FromBody] CreateUpdatePurchaseOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Updating purchase order with ID: {OrderId}", id);
            var order = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, dto);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Purchase order not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update purchase order status - Requires Purchasing.Write permission
    /// </summary>
    [HttpPatch("{id}/status/{status}")]
    [HasPermission("Purchasing", "Write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> UpdatePurchaseOrderStatus(Guid id, string status)
    {
        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var orderStatus))
        {
            _logger.LogWarning("Invalid order status: {Status}", status);
            return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames(typeof(PurchaseOrderStatus)))}");
        }

        try
        {
            _logger.LogInformation("Updating purchase order status with ID: {OrderId} to {Status}", id, status);
            var order = await _purchaseOrderService.UpdatePurchaseOrderStatusAsync(id, orderStatus);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Purchase order not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a purchase order - Requires Purchasing.Delete permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("Purchasing", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePurchaseOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting purchase order with ID: {OrderId}", id);
            await _purchaseOrderService.DeletePurchaseOrderAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Purchase order not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
    }
}
