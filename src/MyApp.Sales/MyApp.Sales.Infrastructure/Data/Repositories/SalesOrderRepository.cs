using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Sales.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Provides Sales Order Repository functionality.
    /// </summary>
    public class SalesOrderRepository : Repository<SalesOrder, Guid>, ISalesOrderRepository
    {
        private readonly SalesDbContext _context;

        /// <summary>base.</summary>
        public SalesOrderRepository(SalesDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>Get By Id Async.</summary>
        public override async Task<SalesOrder?> GetByIdAsync(Guid id)
        {
            return await _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>List Async.</summary>
        public async Task<IEnumerable<SalesOrder>> ListAsync()
        {
            return await _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .ToListAsync();
        }

        /// <summary>Get All Async.</summary>
        public override async Task<IEnumerable<SalesOrder>> GetAllAsync()
        {
            return await ListAsync();
        }

        /// <summary>Get All Paginated Async.</summary>
        public async Task<PaginatedResult<SalesOrder>> GetAllPaginatedAsync(int pageNumber, int pageSize)
        {
            return await base.GetAllPaginatedAsync(pageNumber, pageSize, [o => o.Lines, o => o.Customer]);
        }

        /// <summary>Delete Async.</summary>
        public async Task DeleteAsync(Guid id)
        {
            var order = await _context.SalesOrders.FindAsync(id);
            if (order != null)
            {
                _context.SalesOrders.Remove(order);
            }
        }

        public async Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber)
        {
            return await _context.SalesOrders
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }
    }
}
