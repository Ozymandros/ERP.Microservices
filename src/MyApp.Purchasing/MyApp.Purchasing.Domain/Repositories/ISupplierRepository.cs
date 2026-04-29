using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

/// <summary>
/// Defines the contract for I Supplier Repository.
/// </summary>
public interface ISupplierRepository : IRepository<Supplier, Guid>
{
    Task<Supplier?> GetByEmailAsync(string email);
    Task<IEnumerable<Supplier>> GetByNameAsync(string name);
}
