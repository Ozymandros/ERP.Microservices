using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Infrastructure.Data.Repositories
{
    public class SalesOrderLineRepository : ISalesOrderLineRepository
    {
        private readonly SalesDbContext _context;

        public SalesOrderLineRepository(SalesDbContext context)
        {
            _context = context;
        }

        public async Task<SalesOrderLine?> GetByIdAsync(Guid id)
        {
            return await _context.SalesOrderLines.FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<SalesOrderLine>> ListAsync()
        {
            return await _context.SalesOrderLines.ToListAsync();
        }

        public async Task AddAsync(SalesOrderLine entity)
        {
            await _context.SalesOrderLines.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SalesOrderLine entity)
        {
            _context.SalesOrderLines.Update(entity);
            await _context.SaveChangesAsync();
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
}
