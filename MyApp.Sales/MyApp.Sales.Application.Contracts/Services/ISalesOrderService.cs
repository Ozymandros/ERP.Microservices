using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Sales.Application.Contracts.DTOs;

namespace MyApp.Sales.Application.Contracts.Services
{
    public interface ISalesOrderService
    {
        Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id);
        Task<IEnumerable<SalesOrderDto>> ListSalesOrdersAsync();
        Task<SalesOrderDto> CreateSalesOrderAsync(CreateUpdateSalesOrderDto dto);
        Task<SalesOrderDto> UpdateSalesOrderAsync(Guid id, CreateUpdateSalesOrderDto dto);
        Task DeleteSalesOrderAsync(Guid id);
    }
}
