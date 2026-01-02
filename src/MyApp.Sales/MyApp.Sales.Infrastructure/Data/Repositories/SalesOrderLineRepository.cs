using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Sales.Infrastructure.Data.Repositories;

public class SalesOrderLineRepository : Repository<SalesOrderLine, Guid>, ISalesOrderLineRepository
{
    private readonly SalesDbContext _context;

    public SalesOrderLineRepository(SalesDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<SalesOrderLine?> GetByIdAsync(Guid id)
    {
        return await _context.SalesOrderLines.FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<SalesOrderLine>> ListAsync()
    {
        return await _context.SalesOrderLines.ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var line = await _context.SalesOrderLines.FindAsync(id);
        if (line != null)
        {
            _context.SalesOrderLines.Remove(line);
            await _context.SaveChangesAsync();
        }
    }
}
