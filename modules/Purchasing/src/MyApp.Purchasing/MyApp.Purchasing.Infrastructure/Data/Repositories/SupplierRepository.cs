using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;

namespace MyApp.Purchasing.Infrastructure.Data.Repositories;

public class SupplierRepository : Repository<Supplier, Guid>, ISupplierRepository
{
    public SupplierRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Supplier?> GetByEmailAsync(string email)
    {
        return await _dbContext.Set<Supplier>()
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<IEnumerable<Supplier>> GetByNameAsync(string name)
    {
        return await _dbContext.Set<Supplier>()
            .Where(x => x.Name.Contains(name))
            .ToListAsync();
    }
}
