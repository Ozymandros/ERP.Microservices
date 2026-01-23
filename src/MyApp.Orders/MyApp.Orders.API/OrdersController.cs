using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
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
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
        {
            var orders = await _orderService.ListAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Get operational order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetById(Guid id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
                return NotFound();
            
            return Ok(order);
        }

        /// <summary>
        /// Create a new operational order (Transfer, Inbound, Outbound, Return)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateUpdateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _orderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Create operational order with automatic stock reservation
        /// </summary>
        [HttpPost("with-reservation")]
        public async Task<ActionResult<OrderDto>> CreateWithReservation([FromBody] CreateOrderWithReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _orderService.CreateOrderWithReservationAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Update operational order
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _orderService.UpdateAsync(id, dto);
            return NoContent();
        }

        /// <summary>
        /// Fulfill operational order (mark as completed)
        /// </summary>
        [HttpPost("{id}/fulfill")]
        public async Task<ActionResult<OrderDto>> Fulfill(Guid id, [FromBody] FulfillOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto = dto with { OrderId = id };
            var fulfilled = await _orderService.FulfillOrderAsync(dto);
            return Ok(fulfilled);
        }

        /// <summary>
        /// Cancel operational order
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto = dto with { OrderId = id };
            await _orderService.CancelOrderAsync(dto);
            return NoContent();
        }

        /// <summary>
        /// Delete operational order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _orderService.DeleteAsync(id);
            return NoContent();
        }
    }
}
