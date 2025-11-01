using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

public interface ISupplierRepository : IRepository<Supplier, Guid>
{
    Task<Supplier?> GetByEmailAsync(string email);
    Task<IEnumerable<Supplier>> GetByNameAsync(string name);
}
