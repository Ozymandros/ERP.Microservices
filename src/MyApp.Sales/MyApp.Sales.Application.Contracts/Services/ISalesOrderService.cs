using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Contracts.Services
{
    public interface ISalesOrderService
    {
        Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id);
        Task<IEnumerable<SalesOrderDto>> ListSalesOrdersAsync();
        Task<PaginatedResult<SalesOrderDto>> ListSalesOrdersPaginatedAsync(int pageNumber, int pageSize);
        Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec);
        Task<SalesOrderDto> CreateSalesOrderAsync(CreateUpdateSalesOrderDto dto);
        Task<SalesOrderDto> UpdateSalesOrderAsync(Guid id, CreateUpdateSalesOrderDto dto);
        Task DeleteSalesOrderAsync(Guid id);
    }
}
