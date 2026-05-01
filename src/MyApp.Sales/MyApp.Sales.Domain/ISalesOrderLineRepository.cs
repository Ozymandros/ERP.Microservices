using System;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Domain
{
    /// <summary>
    /// Defines the contract for I Sales Order Line Repository.
    /// </summary>
    public interface ISalesOrderLineRepository : IRepository<SalesOrderLine, Guid>
    {
    }
}
