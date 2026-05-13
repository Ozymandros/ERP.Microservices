using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Repositories;
using System.Linq.Expressions;

namespace MyApp.Sales.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Provides Customer Repository functionality.
    /// </summary>
    public class CustomerRepository : Repository<Customer, Guid>, ICustomerRepository
    {
        private readonly SalesDbContext _context;

        /// <summary>base.</summary>
        public CustomerRepository(SalesDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>Get By Id Async.</summary>
        public override async Task<Customer?> GetByIdAsync(Guid id)
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>List Async.</summary>
        public async Task<IEnumerable<Customer>> ListAsync()
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();
        }

        /// <summary>Get All Async.</summary>
        public override async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await ListAsync();
        }

        /// <summary>Get All Paginated Async.</summary>
        public override async Task<PaginatedResult<Customer>> GetAllPaginatedAsync(int pageNumber, int pageSize, IEnumerable<Expression<Func<Customer, object>>>? includes = null)
        {
            var paginationParams = new PaginationParams(pageNumber, pageSize);
            IQueryable<Customer> query = _context.Customers.Include(c => c.Orders);

            if (includes is not null)
                foreach (var includeExpression in includes)
                {
                    query = query.Include(includeExpression);
                }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PaginatedResult<Customer>(items, paginationParams.PageNumber, paginationParams.PageSize, totalCount);
        }

        /// <summary>Delete Async.</summary>
        public async Task DeleteAsync(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Customer?> GetByNameAsync(string name)
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Email == email);
        }
    }
}
