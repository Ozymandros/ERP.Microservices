using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Orders.Infrastructure.Repositories
{
    /// <summary>
    /// Provides Order Repository functionality.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly OrdersDbContext _db;

        public OrderRepository(OrdersDbContext db)
        {
            _db = db;
        }

        /// <summary>Add Async.</summary>
        public async Task AddAsync(Order entity)
        {
            await _db.Orders.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        /// <summary>Delete Async.</summary>
        public async Task DeleteAsync(Guid id)
        {
            var existing = await _db.Orders.FindAsync(id);
            if (existing != null)
            {
                _db.Orders.Remove(existing);
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>Get By Id Async.</summary>
        public async Task<Order?> GetByIdAsync(Guid id)
        {
            return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>List Async.</summary>
        public async Task<IEnumerable<Order>> ListAsync()
        {
            return await _db.Orders.Include(o => o.Lines).ToListAsync();
        }

        /// <summary>Update Async.</summary>
        public async Task UpdateAsync(Order entity)
        {
            _db.Orders.Update(entity);
            await _db.SaveChangesAsync();
        }

        /// <summary>Query Async.</summary>
        public async Task<PaginatedResult<Order>> QueryAsync(ISpecification<Order> spec)
        {
            var baseQuery = _db.Orders.Include(o => o.Lines).AsQueryable();
            var totalCount = await baseQuery.CountAsync();
            var paginatedQuery = spec.Apply(baseQuery);
            var items = await paginatedQuery.ToListAsync();
            // Extract pagination info from spec if needed
            return new PaginatedResult<Order>(
                items,
                (spec as BaseSpecification<Order>)?.Query.Page ?? 1,
                (spec as BaseSpecification<Order>)?.Query.PageSize ?? items.Count,
                totalCount
            );
        }
    }
}
