using System;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Domain
{
    /// <summary>
    /// Defines the contract for I Customer Repository.
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer, Guid>
    {
        Task<Customer?> GetByNameAsync(string name);
        Task<Customer?> GetByEmailAsync(string email);
    }
}
