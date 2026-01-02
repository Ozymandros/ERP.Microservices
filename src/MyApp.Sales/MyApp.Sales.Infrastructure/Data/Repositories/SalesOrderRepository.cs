using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Sales.Infrastructure.Data.Repositories
{
    public class SalesOrderRepository : Repository<SalesOrder, Guid>, ISalesOrderRepository
    {
        private readonly SalesDbContext _context;

        public SalesOrderRepository(SalesDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<SalesOrder?> GetByIdAsync(Guid id)
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

        public override async Task<IEnumerable<SalesOrder>> GetAllAsync()
        {
            return await ListAsync();
        }

        public override async Task<PaginatedResult<SalesOrder>> GetAllPaginatedAsync(int pageNumber, int pageSize)
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
