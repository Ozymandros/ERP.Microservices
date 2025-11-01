using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Sales.Application.Contracts.Services
{
    public interface ICustomerService
    {
        Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
        Task<IEnumerable<CustomerDto>> ListCustomersAsync();
        Task<PaginatedResult<CustomerDto>> ListCustomersPaginatedAsync(int pageNumber, int pageSize);
        Task<CustomerDto> CreateCustomerAsync(CustomerDto dto);
        Task<CustomerDto> UpdateCustomerAsync(Guid id, CustomerDto dto);
        Task DeleteCustomerAsync(Guid id);
    }
}
