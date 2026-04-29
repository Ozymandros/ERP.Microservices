using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Contracts.Services
{
    /// <summary>
    /// Defines the contract for I Customer Service.
    /// </summary>
    public interface ICustomerService
    {
        Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
        Task<IEnumerable<CustomerDto>> ListCustomersAsync();
        Task<PaginatedResult<CustomerDto>> ListCustomersPaginatedAsync(int pageNumber, int pageSize);
        Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec);
        Task<CustomerDto> CreateCustomerAsync(CustomerDto dto);
        Task<CustomerDto> UpdateCustomerAsync(Guid id, CustomerDto dto);
        Task DeleteCustomerAsync(Guid id);
    }
}
