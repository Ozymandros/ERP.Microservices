using Microsoft.AspNetCore.Mvc;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MyApp.Orders.API
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        readonly IOrderService _orderService;
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: api/<OrdersController>
        /// <summary>Get all orders - Requires Orders.Read permission</summary>
        [HttpGet]
        [HasPermission("Orders", "Read")]
        public async Task<IEnumerable<OrderDto>> Get()
        {
            return await _orderService.ListAsync();
        }

        // GET api/<OrdersController>/5
        /// <summary>Get order by ID - Requires Orders.Read permission</summary>
        [HttpGet("{id}")]
        [HasPermission("Orders", "Read")]
        public async Task<OrderDto> Get(Guid id)
        {
            return await _orderService.GetByIdAsync(id);
        }

        // POST api/<OrdersController>
        /// <summary>Create new order - Requires Orders.Write permission</summary>
        [HttpPost]
        [HasPermission("Orders", "Write")]
        public async Task<OrderDto> Post([FromBody] CreateUpdateOrderDto value)
        {
            return await _orderService.CreateAsync(value);
        }

        // PUT api/<OrdersController>/5
        /// <summary>Update order - Requires Orders.Write permission</summary>
        [HttpPut("{id}")]
        [HasPermission("Orders", "Write")]
        public async Task Put(Guid id, [FromBody] CreateUpdateOrderDto value)
        {
            await _orderService.UpdateAsync(id, value);
        }

        // DELETE api/<OrdersController>/5
        /// <summary>Delete order - Requires Orders.Delete permission</summary>
        [HttpDelete("{id}")]
        [HasPermission("Orders", "Delete")]
        public async Task Delete(Guid id)
        {
            await _orderService.DeleteAsync(id);
        }
    }
}
