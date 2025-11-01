using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Sales.Application.Contracts.Services
{
    public interface ISalesOrderService
    {
        Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id);
        Task<IEnumerable<SalesOrderDto>> ListSalesOrdersAsync();
        Task<PaginatedResult<SalesOrderDto>> ListSalesOrdersPaginatedAsync(int pageNumber, int pageSize);
        Task<SalesOrderDto> CreateSalesOrderAsync(CreateUpdateSalesOrderDto dto);
        Task<SalesOrderDto> UpdateSalesOrderAsync(Guid id, CreateUpdateSalesOrderDto dto);
        Task DeleteSalesOrderAsync(Guid id);
    }
}
