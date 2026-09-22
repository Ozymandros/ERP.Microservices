using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;

namespace MyApp.Purchasing.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Supplier Repository functionality.
/// </summary>
public class SupplierRepository : Repository<Supplier, Guid>, ISupplierRepository
{
    /// <summary>base.</summary>
    /// <param name="dbContext">The db Context.</param>
    public SupplierRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Email Async.</summary>
    /// <param name="email">The email.</param>
    public async Task<Supplier?> GetByEmailAsync(string email)
    {
        return await DbContext.Set<Supplier>()
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    /// <summary>Get By Name Async.</summary>
    /// <param name="name">The name.</param>
    public async Task<IEnumerable<Supplier>> GetByNameAsync(string name)
    {
        return await DbContext.Set<Supplier>()
            .Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
    }
}
