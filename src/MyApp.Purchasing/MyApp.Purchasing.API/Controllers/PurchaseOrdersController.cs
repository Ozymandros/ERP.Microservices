using Microsoft.AspNetCore.Mvc;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Domain.Permissions;
using MyApp.Purchasing.Domain.Specifications;


using MyApp.Shared.Infrastructure.Export;
namespace MyApp.Purchasing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/purchasing/orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService, ICacheService cacheService, ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Export all purchase orders as XLSX
    /// </summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Purchasing", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var orders = await _cacheService.GetStateAsync<IEnumerable<PurchaseOrderDto>>("all_purchase_orders")
                ?? await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            var bytes = orders.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PurchaseOrders.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting purchase orders to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting purchase orders" });
        }
    }

    /// <summary>
    /// Export all purchase orders as PDF
    /// </summary>
    [HttpGet("export-pdf")]
    [HasPermission("Purchasing", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var orders = await _cacheService.GetStateAsync<IEnumerable<PurchaseOrderDto>>("all_purchase_orders")
                ?? await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            var bytes = orders.ExportToPdf();
            return File(bytes, "application/pdf", "PurchaseOrders.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting purchase orders to PDF");
            return StatusCode(500, new { message = "An error occurred exporting purchase orders" });
        }
    }

    /// <summary>
    /// Get all purchase orders - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetAllPurchaseOrders()
    {
        try
        {
            var orders = await _cacheService.GetStateAsync<IEnumerable<PurchaseOrderDto>>("all_purchase_orders");
            if (orders != null)
            {
                _logger.LogInformation("Retrieved all purchase orders from cache");
                return Ok(orders);
            }

            orders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            await _cacheService.SaveStateAsync("all_purchase_orders", orders);
            _logger.LogInformation("Retrieved all purchase orders from database and cached");
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all purchase orders");
            var orders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            return Ok(orders);
        }
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
        try
        {
            string cacheKey = "PurchaseOrder-" + id;
            var order = await _cacheService.GetStateAsync<PurchaseOrderDto>(cacheKey);

            if (order != null)
            {
                _logger.LogInformation("Retrieved purchase order {@Order} from cache", new { OrderId = id });
                return Ok(order);
            }

            order = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Purchase order with ID {@Order} not found", new { OrderId = id });
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, order);
            _logger.LogInformation("Retrieved purchase order {@Order} from database and cached", new { OrderId = id });
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order {@Order}", new { OrderId = id });
            var order = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            return order == null ? NotFound() : Ok(order);
        }
    }

    /// <summary>
    /// Search purchase orders with advanced filtering, sorting, and pagination - Requires Purchasing.Read permission
    /// </summary>
    /// <remarks>
    /// Supported filters: orderNumber, supplierId, status, minTotal, maxTotal
    /// Supported sort fields: id, orderNumber, status, totalAmount, createdAt, orderDate
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<PurchaseOrderDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            query.Validate();
            var spec = new PurchaseOrderQuerySpec(query);
            var result = await _purchaseOrderService.QueryPurchaseOrdersAsync(spec);
            _logger.LogInformation("Searched purchase orders with query: {@Query}", query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching purchase orders");
            return StatusCode(500, new { message = "An error occurred searching purchase orders" });
        }
    }

    /// <summary>
    /// Get purchase orders by supplier - Requires Purchasing.Read permission
    /// </summary>
    [HttpGet("supplier/{supplierId}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrdersBySupplier(Guid supplierId)
    {
        _logger.LogInformation("Retrieving purchase orders for supplier: {@Supplier}", new { SupplierId = supplierId });
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
            _logger.LogWarning("Invalid order status: {@Status}", new { Status = status });
            return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames(typeof(PurchaseOrderStatus)))}");
        }

        _logger.LogInformation("Retrieving purchase orders with status: {@Status}", new { Status = status });
        var orders = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(orderStatus);
        return Ok(orders);
    }

    /// <summary>
    /// Create a new purchase order - Requires Purchasing.Create permission
    /// </summary>
    [HttpPost]
    [HasPermission("Purchasing", "Create")]
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
            _logger.LogInformation("Creating new purchase order: {@Order}", new { OrderNumber = dto.OrderNumber });
            var order = await _purchaseOrderService.CreatePurchaseOrderAsync(dto);
            await _cacheService.RemoveStateAsync("all_purchase_orders");
            _logger.LogInformation("Purchase order {@Order} created and cache invalidated", new { OrderId = order.Id });
            return CreatedAtAction(nameof(GetPurchaseOrderById), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Purchase order not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing purchase order - Requires Purchasing.Update permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Purchasing", "Update")]
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
            _logger.LogInformation("Updating purchase order with ID: {@Order}", new { OrderId = id });
            var order = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, dto);
            string cacheKey = "PurchaseOrder-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_purchase_orders");
            _logger.LogInformation("Purchase order {@Order} updated and cache invalidated", new { OrderId = id });
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Purchase order not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update purchase order status - Requires Purchasing.Update permission
    /// </summary>
    [HttpPatch("{id}/status/{status}")]
    [HasPermission("Purchasing", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> UpdatePurchaseOrderStatus(Guid id, string status)
    {
        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var orderStatus))
        {
            _logger.LogWarning("Invalid order status: {@Status}", new { Status = status });
            return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames(typeof(PurchaseOrderStatus)))}");
        }

        try
        {
            _logger.LogInformation("Updating purchase order status with ID: {@Order} to {@Status}", new { OrderId = id }, new { Status = status });
            var order = await _purchaseOrderService.UpdatePurchaseOrderStatusAsync(id, orderStatus);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Purchase order not found: {@Error}", new { Message = ex.Message });
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
            _logger.LogInformation("Deleting purchase order with ID: {@Order}", new { OrderId = id });
            await _purchaseOrderService.DeletePurchaseOrderAsync(id);
            string cacheKey = "PurchaseOrder-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_purchase_orders");
            _logger.LogInformation("Purchase order {@Order} deleted and cache invalidated", new { OrderId = id });
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Purchase order not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Approve a purchase order - Requires Purchasing.Update permission
    /// </summary>
    [HttpPost("{id}/approve")]
    [HasPermission("Purchasing", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> ApprovePurchaseOrder(Guid id, [FromBody] ApprovePurchaseOrderDto dto)
    {
        if (id != dto.PurchaseOrderId)
        {
            return BadRequest(new { error = "Purchase order ID mismatch" });
        }

        try
        {
            _logger.LogInformation("Approving purchase order: {@Order}", new { OrderId = id });
            var order = await _purchaseOrderService.ApprovePurchaseOrderAsync(dto);
            string cacheKey = "PurchaseOrder-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_purchase_orders");
            _logger.LogInformation("Purchase order {@Order} approved and cache invalidated", new { OrderId = id });
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Purchase order not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {@Error}", new { Message = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Receive a purchase order - Requires Purchasing.Update permission
    /// </summary>
    [HttpPost("{id}/receive")]
    [HasPermission("Purchasing", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> ReceivePurchaseOrder(Guid id, [FromBody] ReceivePurchaseOrderDto dto)
    {
        if (id != dto.PurchaseOrderId)
        {
            return BadRequest(new { error = "Purchase order ID mismatch" });
        }

        try
        {
            _logger.LogInformation(
                "Receiving purchase order: {@Order}, Warehouse={WarehouseId}",
                new { OrderId = id }, dto.WarehouseId);
            var order = await _purchaseOrderService.ReceivePurchaseOrderAsync(dto);
            string cacheKey = "PurchaseOrder-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_purchase_orders");
            _logger.LogInformation("Purchase order {@Order} received and cache invalidated", new { OrderId = id });
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Purchase order not found: {@Error}", new { Message = ex.Message });
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {@Error}", new { Message = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
    }
}
