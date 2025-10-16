using System;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Domain
{
    public interface ISalesOrderLineRepository : IRepository<SalesOrderLine, Guid>
    {
    }
}
