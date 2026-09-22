using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Contracts.Services
{
    /// <summary>Defines the contract for customer management operations within the Sales service.</summary>
public interface ICustomerService
{
    /// <summary>Returns the customer with the given identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The unique identifier of the customer.</param>
    /// <returns>The matching <see cref="CustomerDto"/>, or <see langword="null"/>.</returns>
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);

    /// <summary>Returns the customer with the given name, or <see langword="null"/> if not found.</summary>
    /// <param name="name">The exact customer name to search for.</param>
    /// <returns>The matching <see cref="CustomerDto"/>, or <see langword="null"/>.</returns>
    Task<CustomerDto?> GetCustomerByNameAsync(string name);

    /// <summary>Returns the customer with the given email address, or <see langword="null"/> if not found.</summary>
    /// <param name="email">The exact email address to search for.</param>
    /// <returns>The matching <see cref="CustomerDto"/>, or <see langword="null"/>.</returns>
    Task<CustomerDto?> GetCustomerByEmailAsync(string email);

    /// <summary>Returns all customers as an enumerable sequence.</summary>
    /// <returns>All <see cref="CustomerDto"/> records.</returns>
    Task<IEnumerable<CustomerDto>> ListCustomersAsync();

    /// <summary>Returns a paginated list of customers.</summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PaginatedResult{T}"/> containing the requested page of customers.</returns>
    Task<PaginatedResult<CustomerDto>> ListCustomersPaginatedAsync(int pageNumber, int pageSize);

    /// <summary>Returns a paginated list of customers that satisfy the given specification.</summary>
    /// <param name="spec">The specification defining filtering, sorting, and pagination criteria.</param>
    /// <returns>A <see cref="PaginatedResult{T}"/> containing matching customers.</returns>
    Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec);

    /// <summary>Creates a new customer from the given data.</summary>
    /// <param name="dto">The customer data to create.</param>
    /// <returns>The created <see cref="CustomerDto"/> with generated identifier and audit fields.</returns>
    Task<CustomerDto> CreateCustomerAsync(CustomerDto dto);

    /// <summary>Updates the customer with the given identifier using the provided data.</summary>
    /// <param name="id">The unique identifier of the customer to update.</param>
    /// <param name="dto">The updated customer data.</param>
    /// <returns>The updated <see cref="CustomerDto"/>.</returns>
    Task<CustomerDto> UpdateCustomerAsync(Guid id, CreateUpdateCustomerDto dto);

    /// <summary>Deletes the customer with the given identifier.</summary>
    /// <param name="id">The unique identifier of the customer to delete.</param>
    Task DeleteCustomerAsync(Guid id);
}
}
