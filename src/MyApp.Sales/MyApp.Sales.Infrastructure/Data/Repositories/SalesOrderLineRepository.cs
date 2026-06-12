using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Sales.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Sales Order Line Repository functionality.
/// </summary>
public class SalesOrderLineRepository : Repository<SalesOrderLine, Guid>, ISalesOrderLineRepository
{
    private readonly SalesDbContext _context;

    /// <summary>base.</summary>
    public SalesOrderLineRepository(SalesDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    public override async Task<SalesOrderLine?> GetByIdAsync(Guid id)
    {
        return await _context.SalesOrderLines.FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<SalesOrderLine>> ListAsync()
    {
        return await _context.SalesOrderLines.ToListAsync();
    }

    /// <summary>Delete Async.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var line = await _context.SalesOrderLines.FindAsync(id);
        if (line != null)
        {
            _context.SalesOrderLines.Remove(line);
        }
    }
}
