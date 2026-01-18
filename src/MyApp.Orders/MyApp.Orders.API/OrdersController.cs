using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MyApp.Orders.API
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        readonly IOrderService _orderService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ICacheService cacheService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _cacheService = cacheService;
            _logger = logger;
        }

        // GET: api/<OrdersController>
        /// <summary>Get all orders - Requires Orders.Read permission</summary>
        [HttpGet]
        [HasPermission("Orders", "Read")]
        public async Task<IEnumerable<OrderDto>> Get()
        {
            try
            {
                var orders = await _cacheService.GetStateAsync<IEnumerable<OrderDto>>("all_orders");
                if (orders != null)
                {
                    _logger.LogInformation("Retrieved all orders from cache");
                    return orders;
                }

                orders = await _orderService.ListAsync();
                await _cacheService.SaveStateAsync("all_orders", orders);
                _logger.LogInformation("Retrieved all orders from database and cached");
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                return await _orderService.ListAsync();
            }
        }

        // GET api/<OrdersController>/5
        /// <summary>Get order by ID - Requires Orders.Read permission</summary>
        [HttpGet("{id}")]
        [HasPermission("Orders", "Read")]
        public async Task<OrderDto?> Get(Guid id)
        {
            try
            {
                string cacheKey = "Order-" + id;
                var order = await _cacheService.GetStateAsync<OrderDto>(cacheKey);

                if (order is not null)
                {
                    _logger.LogInformation("Retrieved order {@Order} from cache", new { OrderId = id });
                    return order;
                }

                order = await _orderService.GetByIdAsync(id);
                if (order != null)
                {
                    await _cacheService.SaveStateAsync(cacheKey, order);
                    _logger.LogInformation("Retrieved order {@Order} from database and cached", new { OrderId = id });
                }
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {@Order}", new { OrderId = id });
                return await _orderService.GetByIdAsync(id);
            }
        }

        // POST api/<OrdersController>
        /// <summary>Create new order - Requires Orders.Create permission</summary>
        [HttpPost]
        [HasPermission("Orders", "Create")]
        public async Task<OrderDto> Post([FromBody] CreateUpdateOrderDto value)
        {
            try
            {
                var result = await _orderService.CreateAsync(value);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation("Order {@Order} created and cache invalidated", new { OrderId = result.Id });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        // PUT api/<OrdersController>/5
        /// <summary>Update order - Requires Orders.Update permission</summary>
        [HttpPut("{id}")]
        [HasPermission("Orders", "Update")]
        public async Task Put(Guid id, [FromBody] CreateUpdateOrderDto value)
        {
            try
            {
                await _orderService.UpdateAsync(id, value);
                string cacheKey = "Order-" + id;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation("Order {@Order} updated and cache invalidated", new { OrderId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {@Order}", new { OrderId = id });
                throw;
            }
        }

        // DELETE api/<OrdersController>/5
        /// <summary>Delete order - Requires Orders.Delete permission</summary>
        [HttpDelete("{id}")]
        [HasPermission("Orders", "Delete")]
        public async Task Delete(Guid id)
        {
            try
            {
                await _orderService.DeleteAsync(id);
                string cacheKey = "Order-" + id;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation("Order {@Order} deleted and cache invalidated", new { OrderId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {@Order}", new { OrderId = id });
                throw;
            }
        }

        // POST api/<OrdersController>/with-reservation
        /// <summary>Create order with automatic stock reservation - Requires Orders.Create permission</summary>
        [HttpPost("with-reservation")]
        [HasPermission("Orders", "Create")]
        public async Task<ActionResult<OrderDto>> CreateWithReservation([FromBody] CreateOrderWithReservationDto dto)
        {
            try
            {
                var result = await _orderService.CreateOrderWithReservationAsync(dto);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation(
                    "Order {@Order} created with stock reservation and cache invalidated",
                    new { OrderId = result.Id });
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order with reservation");
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST api/<OrdersController>/fulfill
        /// <summary>Fulfill order - Requires Orders.Update permission</summary>
        [HttpPost("fulfill")]
        [HasPermission("Orders", "Update")]
        public async Task<ActionResult<OrderDto>> FulfillOrder([FromBody] FulfillOrderDto dto)
        {
            try
            {
                var result = await _orderService.FulfillOrderAsync(dto);
                string cacheKey = "Order-" + dto.OrderId;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation(
                    "Order {@Order} fulfilled and cache invalidated",
                    new { OrderId = dto.OrderId });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fulfilling order {@Order}", new { OrderId = dto.OrderId });
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST api/<OrdersController>/cancel
        /// <summary>Cancel order - Requires Orders.Update permission</summary>
        [HttpPost("cancel")]
        [HasPermission("Orders", "Update")]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderDto dto)
        {
            try
            {
                await _orderService.CancelOrderAsync(dto);
                string cacheKey = "Order-" + dto.OrderId;
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation(
                    "Order {@Order} cancelled and cache invalidated",
                    new { OrderId = dto.OrderId });
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {@Order}", new { OrderId = dto.OrderId });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
