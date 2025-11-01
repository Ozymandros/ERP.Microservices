using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Sales.Domain
{
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(TKey id);
        Task<IEnumerable<TEntity>> ListAsync();
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(int pageNumber, int pageSize);
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task DeleteAsync(TKey id);
    }
}
