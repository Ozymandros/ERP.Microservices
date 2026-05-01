using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Domain
{
    /// <summary>Generic repository interface for CRUD operations on entities.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>Retrieves an entity by its primary key.</summary>
        Task<TEntity?> GetByIdAsync(TKey id);
        /// <summary>Retrieves all entities.</summary>
        Task<IEnumerable<TEntity>> ListAsync();
        /// <summary>Adds a new entity.</summary>
        Task AddAsync(TEntity entity);
        /// <summary>Updates an existing entity.</summary>
        Task UpdateAsync(TEntity entity);
        /// <summary>Deletes an entity by its primary key.</summary>
        Task DeleteAsync(TKey id);
    }
}
