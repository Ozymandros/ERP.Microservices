using AutoMapper;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IOrderLineRepository _lines;
        private readonly IMapper _mapper;

        public OrderService(IOrderRepository orders, IOrderLineRepository lines, IMapper mapper)
        {
            _orders = orders;
            _lines = lines;
            _mapper = mapper;
        }

        public async Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto)
        {
            var entity = _mapper.Map<Order>(dto);
            entity.Id = Guid.NewGuid();
            entity.OrderDate = dto.OrderDate;
            entity.Status = OrderStatus.Draft;
            entity.TotalAmount = entity.Lines.Sum(l => l.LineTotal = l.Quantity * l.UnitPrice);

            foreach (var line in entity.Lines)
            {
                line.Id = Guid.NewGuid();
                line.OrderId = entity.Id;
            }

            await _orders.AddAsync(entity);

            return _mapper.Map<OrderDto>(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _orders.DeleteAsync(id);
        }

        public async Task<OrderDto> GetByIdAsync(Guid id)
        {
            var entity = await _orders.GetByIdAsync(id);
            return _mapper.Map<OrderDto>(entity!);
        }

        public async Task<IEnumerable<OrderDto>> ListAsync()
        {
            var list = await _orders.ListAsync();
            return list.Select(o => _mapper.Map<OrderDto>(o));
        }

        public async Task UpdateAsync(Guid id, CreateUpdateOrderDto dto)
        {
            var existing = await _orders.GetByIdAsync(id);
            if (existing == null) return;

            existing.OrderNumber = dto.OrderNumber;
            existing.CustomerId = dto.CustomerId;
            existing.OrderDate = dto.OrderDate;

            // Simple handling: replace lines
            existing.Lines.Clear();
            foreach (var l in dto.Lines)
            {
                var line = new OrderLine(Guid.NewGuid())
                {
                    OrderId = existing.Id,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.Quantity * l.UnitPrice
                };
                existing.Lines.Add(line);
            }

            existing.TotalAmount = existing.Lines.Sum(x => x.LineTotal);

            await _orders.UpdateAsync(existing);
        }
    }
}
