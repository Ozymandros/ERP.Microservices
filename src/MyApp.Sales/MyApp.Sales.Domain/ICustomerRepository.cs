using System;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Domain
{
    /// <summary>
    /// Defines the contract for I Customer Repository.
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer, Guid>
    {
        /// <summary>Returns the customer with the specified name, or <see langword="null"/> if not found.</summary>
        /// <param name="name">The exact customer name to search for.</param>
        /// <returns>The matching <see cref="Customer"/>, or <see langword="null"/>.</returns>
        Task<Customer?> GetByNameAsync(string name);

        /// <summary>Returns the customer with the specified email address, or <see langword="null"/> if not found.</summary>
        /// <param name="email">The exact email address to search for.</param>
        /// <returns>The matching <see cref="Customer"/>, or <see langword="null"/>.</returns>
        Task<Customer?> GetByEmailAsync(string email);
    }
}
