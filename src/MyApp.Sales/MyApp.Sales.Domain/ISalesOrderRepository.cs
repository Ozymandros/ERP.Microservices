using System;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Domain
{
    /// <summary>
    /// Defines the contract for I Sales Order Repository.
    /// </summary>
    public interface ISalesOrderRepository : IRepository<SalesOrder, Guid>
    {
    }
}
