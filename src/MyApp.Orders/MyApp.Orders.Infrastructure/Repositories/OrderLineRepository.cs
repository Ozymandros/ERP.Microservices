using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;

namespace MyApp.Orders.Infrastructure.Repositories
{
    public class OrderLineRepository : IOrderLineRepository
    {
        private readonly OrdersDbContext _db;

        public OrderLineRepository(OrdersDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(OrderLine entity)
        {
            await _db.OrderLines.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var existing = await _db.OrderLines.FindAsync(id);
            if (existing != null)
            {
                _db.OrderLines.Remove(existing);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<OrderLine?> GetByIdAsync(Guid id)
        {
            return await _db.OrderLines.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<OrderLine>> ListAsync()
        {
            return await _db.OrderLines.ToListAsync();
        }

        public async Task UpdateAsync(OrderLine entity)
        {
            _db.OrderLines.Update(entity);
            await _db.SaveChangesAsync();
        }
    }
}
