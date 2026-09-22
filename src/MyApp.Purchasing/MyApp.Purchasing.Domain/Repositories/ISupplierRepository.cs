using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

/// <summary>
/// Defines the contract for I Supplier Repository.
/// </summary>
public interface ISupplierRepository : IRepository<Supplier, Guid>
{
    /// <summary>Returns the supplier with the specified email address.</summary>
    /// <param name="email">The email address to look up.</param>
    /// <returns>The matching supplier, or <c>null</c> if not found.</returns>
    Task<Supplier?> GetByEmailAsync(string email);
    /// <summary>Returns all suppliers whose name contains the specified search term.</summary>
    /// <param name="name">The name fragment to search for.</param>
    /// <returns>A collection of matching suppliers.</returns>
    Task<IEnumerable<Supplier>> GetByNameAsync(string name);
}
