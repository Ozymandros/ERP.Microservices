using MyApp.Orders.Application.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApp.Orders.Application.Contracts
{
    public interface IOrderService
    {
        Task<OrderDto> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderDto>> ListAsync();
        Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto);
        Task UpdateAsync(Guid id, CreateUpdateOrderDto dto);
        Task DeleteAsync(Guid id);
    }
}
