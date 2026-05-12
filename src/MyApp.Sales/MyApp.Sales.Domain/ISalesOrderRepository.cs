using System;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Sales.Domain
{
    /// <summary>
    /// Defines the contract for I Sales Order Repository.
    /// </summary>
    public interface ISalesOrderRepository : IRepository<SalesOrder, Guid>
    {
        Task<PaginatedResult<SalesOrder>> GetAllPaginatedAsync(int pageNumber, int pageSize);
        Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber);
    }
}
