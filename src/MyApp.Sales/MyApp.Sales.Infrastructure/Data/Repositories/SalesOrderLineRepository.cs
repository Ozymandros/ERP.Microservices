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
    /// <param name="context">The context.</param>
    public SalesOrderLineRepository(SalesDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Id Async.</summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public override async Task<SalesOrderLine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrderLines.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <summary>List Async.</summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IEnumerable<SalesOrderLine>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrderLines.ToListAsync(cancellationToken);
    }

    /// <summary>Delete Async.</summary>
    /// <param name="id">The id.</param>
    public async Task DeleteAsync(Guid id)
    {
        var line = await _context.SalesOrderLines.FindAsync(id);
        if (line != null)
        {
            _context.SalesOrderLines.Remove(line);
        }
    }
}
