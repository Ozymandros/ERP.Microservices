using Microsoft.AspNetCore.Mvc;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Purchasing.Domain.Specifications;


using MyApp.Shared.Infrastructure.Export;
using MyApp.Shared.Infrastructure.Extensions;
namespace MyApp.Purchasing.API.Controllers;

/// <summary>API controller for managing purchase orders including creation, approval, and receiving workflows.</summary>
[ApiController]
[Authorize]
[Route("api/purchasing/orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    /// <summary>
    /// Initializes a new instance of the PurchaseOrdersController class.
    /// </summary>
    /// <param name="purchaseOrderService">The purchase Order Service.</param>
    /// <param name="cacheService">The cache Service.</param>
    /// <param name="logger">The logger.</param>
    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService, ICacheService cacheService, ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>Exports all purchase orders as an XLSX file.</summary>
    /// <returns>An XLSX file containing all purchase orders.</returns>
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

    /// <summary>Exports all purchase orders as a PDF file.</summary>
    /// <returns>A PDF file containing all purchase orders.</returns>
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

    /// <summary>Retrieves all purchase orders, or a filtered/paginated result when query parameters are present.</summary>
    /// <param name="query">Optional query parameters for filtering, sorting, and paging.</param>
    /// <returns>A list or paginated collection of purchase order DTOs.</returns>
    [HttpGet]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetAllPurchaseOrders([FromQuery] QuerySpec query)
    {
        try
        {
            // If query parameters are provided, perform a search/paginated query
            if (Request.Query.Any())
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var spec = new PurchaseOrderQuerySpec(query);
                var result = await _purchaseOrderService.QueryPurchaseOrdersAsync(spec);
                return Ok(result);
            }

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
            return StatusCode(500, new { message = "An error occurred retrieving purchase orders" });
        }
    }

    /// <summary>Retrieves a purchase order by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the purchase order.</param>
    /// <returns>The purchase order DTO, or 404 if not found.</returns>
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

    /// <summary>Retrieves a purchase order by its order number.</summary>
    /// <param name="orderNumber">The order number to look up.</param>
    /// <returns>The purchase order DTO, or 404 if not found.</returns>
    [HttpGet("code/{orderNumber}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrderByOrderNumber(string orderNumber)
    {
        try
        {
            string cacheKey = "PurchaseOrder-Code-" + orderNumber;
            var order = await _cacheService.GetStateAsync<PurchaseOrderDto>(cacheKey);

            if (order != null)
            {
                _logger.LogInformation("Retrieved purchase order with code {@OrderNumber} from cache", new { OrderNumber = orderNumber });
                return Ok(order);
            }

            order = await _purchaseOrderService.GetPurchaseOrderByOrderNumberAsync(orderNumber);
            if (order == null)
            {
                _logger.LogWarning("Purchase order with code {@OrderNumber} not found", new { OrderNumber = orderNumber });
                return NotFound();
            }

            await _cacheService.SaveStateAsync(cacheKey, order);
            _logger.LogInformation("Retrieved purchase order with code {@OrderNumber} from database and cached", new { OrderNumber = orderNumber });
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order with code {@OrderNumber}", new { OrderNumber = orderNumber });
            var order = await _purchaseOrderService.GetPurchaseOrderByOrderNumberAsync(orderNumber);
            return order == null ? NotFound() : Ok(order);
        }
    }

    /// <summary>Searches purchase orders with advanced filtering, sorting, and pagination.</summary>
    /// <param name="query">Query parameters for filtering, sorting, and paging.</param>
    /// <returns>A paginated result of matching purchase order DTOs.</returns>
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
            query.BindFiltersFromQuery(Request.Query);
            query.Validate();
            var spec = new PurchaseOrderQuerySpec(query);
            var result = await _purchaseOrderService.QueryPurchaseOrdersAsync(spec);
            _logger.LogInformation("Searched purchase orders");
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

    /// <summary>Retrieves all purchase orders for a specific supplier.</summary>
    /// <param name="supplierId">The unique identifier of the supplier.</param>
    /// <returns>A collection of purchase order DTOs for the supplier.</returns>
    [HttpGet("supplier/{supplierId}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrdersBySupplier(Guid supplierId)
    {
        _logger.LogInformation("Retrieving purchase orders for supplier: {@Supplier}", new { SupplierId = supplierId });
        var orders = await _purchaseOrderService.GetPurchaseOrdersBySupplierAsync(supplierId);
        return Ok(orders);
    }

    /// <summary>Retrieves all purchase orders with the specified status.</summary>
    /// <param name="status">The status string to filter by (Draft, Approved, Received, Cancelled).</param>
    /// <returns>A collection of purchase order DTOs with the given status.</returns>
    [HttpGet("status/{status}")]
    [HasPermission("Purchasing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrdersByStatus(string status)
    {
        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var orderStatus))
        {
            _logger.LogWarning("Invalid order status: {@Status}", new { Status = status });
            return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames<PurchaseOrderStatus>())}");
        }

        _logger.LogInformation("Retrieving purchase orders with status: {@Status}", new { Status = status });
        var orders = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(orderStatus);
        return Ok(orders);
    }

    /// <summary>Creates a new purchase order.</summary>
    /// <param name="dto">The data for the new purchase order.</param>
    /// <returns>The created purchase order DTO with a 201 Created response.</returns>
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
            _logger.LogInformation("Creating new purchase order: {@Order}", new { SupplierId = dto.SupplierId });
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

    /// <summary>Updates an existing purchase order.</summary>
    /// <param name="id">The unique identifier of the purchase order to update.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>The updated purchase order DTO.</returns>
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

    /// <summary>Updates the status of a purchase order.</summary>
    /// <param name="id">The unique identifier of the purchase order.</param>
    /// <param name="status">The new status string (Draft, Approved, Received, Cancelled).</param>
    /// <returns>The updated purchase order DTO.</returns>
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
            return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames<PurchaseOrderStatus>())}");
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

    /// <summary>Deletes a purchase order by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the purchase order to delete.</param>
    /// <returns>204 No Content on success, or 404 if not found.</returns>
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

    /// <summary>Approves a purchase order for processing.</summary>
    /// <param name="id">The unique identifier of the purchase order to approve.</param>
    /// <param name="dto">The approval details.</param>
    /// <returns>The approved purchase order DTO.</returns>
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

    /// <summary>Records the receipt of goods for a purchase order and creates an inbound operational order.</summary>
    /// <param name="id">The unique identifier of the purchase order to receive.</param>
    /// <param name="dto">The receiving details including warehouse and per-line quantities.</param>
    /// <returns>The updated purchase order DTO reflecting received status.</returns>
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
