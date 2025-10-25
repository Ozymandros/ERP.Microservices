using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Infrastructure.Data.Repositories
{
    public class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly SalesDbContext _context;

        public SalesOrderRepository(SalesDbContext context)
        {
            _context = context;
        }

        public async Task<SalesOrder?> GetByIdAsync(Guid id)
        {
            return await _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<SalesOrder>> ListAsync()
        {
            return await _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .ToListAsync();
        }

        public async Task AddAsync(SalesOrder entity)
        {
            await _context.SalesOrders.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SalesOrder entity)
        {
            _context.SalesOrders.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var order = await _context.SalesOrders.FindAsync(id);
            if (order != null)
            {
                _context.SalesOrders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}
