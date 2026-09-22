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
        /// <summary>Returns a paginated list of all sales orders.</summary>
        /// <param name="pageNumber">The 1-based page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A <see cref="PaginatedResult{T}"/> containing sales orders for the requested page.</returns>
        Task<PaginatedResult<SalesOrder>> GetAllPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>Returns the sales order with the given order number, or <see langword="null"/> if not found.</summary>
        /// <param name="orderNumber">The order number to search for.</param>
        /// <returns>The matching <see cref="SalesOrder"/>, or <see langword="null"/>.</returns>
        Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber);
    }
}
