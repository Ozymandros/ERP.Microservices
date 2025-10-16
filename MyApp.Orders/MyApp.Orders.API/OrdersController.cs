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
        [HttpGet]
        public async Task<IEnumerable<OrderDto>> Get()
        {
            return await _orderService.ListAsync();
        }

        // GET api/<OrdersController>/5
        [HttpGet("{id}")]
        public async Task<OrderDto> Get(Guid id)
        {
            return await _orderService.GetByIdAsync(id);
        }

        // POST api/<OrdersController>
        [HttpPost]
        public async Task<OrderDto> Post([FromBody] CreateUpdateOrderDto value)
        {
            return await _orderService.CreateAsync(value);
        }

        // PUT api/<OrdersController>/5
        [HttpPut("{id}")]
        public async Task Put(Guid id, [FromBody] CreateUpdateOrderDto value)
        {
            await _orderService.UpdateAsync(id, value);
        }

        // DELETE api/<OrdersController>/5
        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            await _orderService.DeleteAsync(id);
        }
    }
}
