using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Shared.Domain.Caching;

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
                string cacheKey = $"Order-{id}";
                var order = await _cacheService.GetStateAsync<OrderDto>(cacheKey);

                if (order is not null)
                {
                    _logger.LogInformation("Retrieved order {OrderId} from cache", id);
                    return order;
                }

                order = await _orderService.GetByIdAsync(id);
                if (order != null)
                {
                    await _cacheService.SaveStateAsync(cacheKey, order);
                    _logger.LogInformation("Retrieved order {OrderId} from database and cached", id);
                }
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
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
                _logger.LogInformation("Order created and cache invalidated");
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
                string cacheKey = $"Order-{id}";
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation("Order {OrderId} updated and cache invalidated", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
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
                string cacheKey = $"Order-{id}";
                await _cacheService.RemoveStateAsync(cacheKey);
                await _cacheService.RemoveStateAsync("all_orders");
                _logger.LogInformation("Order {OrderId} deleted and cache invalidated", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                throw;
            }
        }
    }
}
