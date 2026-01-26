using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Orders.Domain.Specifications;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Orders.API
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ICacheService cacheService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Export all operational orders as XLSX
        /// </summary>
        [HttpGet("export-xlsx")]
        [HasPermission("Orders", "Read")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToXlsx()
        {
            try
            {
                var orders = await _cacheService.GetStateAsync<IEnumerable<OrderDto>>("all_orders")
                    ?? await _orderService.ListAsync();
                var bytes = orders.ExportToXlsx();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders to XLSX");
                return StatusCode(500, new { message = "An error occurred exporting orders" });
            }
        }

        /// <summary>
        /// Export all operational orders as PDF
        /// </summary>
        [HttpGet("export-pdf")]
        [HasPermission("Orders", "Read")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToPdf()
        {
            try
            {
                var orders = await _cacheService.GetStateAsync<IEnumerable<OrderDto>>("all_orders")
                    ?? await _orderService.ListAsync();
                var bytes = orders.ExportToPdf();
                return File(bytes, "application/pdf", "Orders.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders to PDF");
                return StatusCode(500, new { message = "An error occurred exporting orders" });
            }
        }

        /// <summary>
        /// Get all operational orders
        /// </summary>
        [HttpGet]
        [HasPermission("Orders", "Read")]
        [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var orders = await _orderService.ListAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                var orders = await _orderService.ListAsync();
                return Ok(orders);
            }
        }

        /// <summary>
        /// Get operational order by ID
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission("Orders", "Read")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var order = await _orderService.GetByIdAsync(id);
                if (order == null)
                    return NotFound(new { message = $"Order with ID {id} not found." });
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {@Order}", new { OrderId = id });
                var order = await _orderService.GetByIdAsync(id);
                return order == null ? NotFound(new { message = $"Order with ID {id} not found." }) : Ok(order);
            }
        }

        /// <summary>
        /// Create a new operational order (Transfer, Inbound, Outbound, Return)
        /// </summary>
        [HttpPost]
        [HasPermission("Orders", "Create")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateUpdateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _orderService.CreateAsync(dto);
                await _cacheService.RemoveStateAsync("all_orders");
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        /// <summary>
        /// Create operational order with automatic stock reservation
        /// </summary>
        [HttpPost("with-reservation")]
        [HasPermission("Orders", "Create")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateWithReservation([FromBody] CreateOrderWithReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _orderService.CreateOrderWithReservationAsync(dto);
                await _cacheService.RemoveStateAsync("all_orders");
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order with reservation");
                throw;
            }
        }

        /// <summary>
        /// Update operational order
        /// </summary>
        [HttpPut("{id}")]
        [HasPermission("Orders", "Update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _orderService.UpdateAsync(id, dto);
                await _cacheService.RemoveStateAsync("all_orders");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error updating order {@Order}: {@Error}", new { OrderId = id }, new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                throw;
            }
        }

        /// <summary>
        /// Fulfill operational order (mark as completed)
        /// </summary>
        [HttpPost("{id}/fulfill")]
        [HasPermission("Orders", "Update")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Fulfill(Guid id, [FromBody] FulfillOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                dto = dto with { OrderId = id };
                var fulfilled = await _orderService.FulfillOrderAsync(dto);
                return Ok(fulfilled);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error fulfilling order {@Order}: {@Error}", new { OrderId = id }, new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fulfilling order");
                throw;
            }
        }

        /// <summary>
        /// Cancel operational order
        /// </summary>
        [HttpPost("{id}/cancel")]
        [HasPermission("Orders", "Update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                dto = dto with { OrderId = id };
                await _orderService.CancelOrderAsync(dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error cancelling order {@Order}: {@Error}", new { OrderId = id }, new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                throw;
            }
        }

        /// <summary>
        /// Delete operational order
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission("Orders", "Delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _orderService.DeleteAsync(id);
                await _cacheService.RemoveStateAsync("all_orders");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error deleting order {@Order}: {@Error}", new { OrderId = id }, new { Message = ex.Message });
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                throw;
            }
        }

        /// <summary>
        /// Search operational orders with filter, sort, and pagination
        /// </summary>
        [HttpGet("search")]
        [HasPermission("Orders", "Read")]
        [ProducesResponseType(typeof(PaginatedResult<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromQuery] QuerySpec query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                query.Validate();
                var spec = new OrderQuerySpec(query);
                var result = await _orderService.QueryOrdersAsync(spec);
                _logger.LogInformation("Searched orders with query: {@Query}", query);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid query specification");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching orders");
                return StatusCode(500, new { message = "An error occurred searching orders" });
            }
        }
    }
}
