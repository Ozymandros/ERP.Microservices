using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;

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

        public async Task<IEnumerable<SalesOrder>> GetAllAsync()
        {
            return await _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .ToListAsync();
        }

        public async Task<PaginatedResult<SalesOrder>> GetAllPaginatedAsync(int pageNumber, int pageSize)
        {
            var paginationParams = new PaginationParams(pageNumber, pageSize);
            var query = _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer);
            
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PaginatedResult<SalesOrder>(items, paginationParams.PageNumber, paginationParams.PageSize, totalCount);
        }

        public async Task<SalesOrder> AddAsync(SalesOrder entity)
        {
            await _context.SalesOrders.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<SalesOrder> UpdateAsync(SalesOrder entity)
        {
            _context.SalesOrders.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(SalesOrder entity)
        {
            _context.SalesOrders.Remove(entity);
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
