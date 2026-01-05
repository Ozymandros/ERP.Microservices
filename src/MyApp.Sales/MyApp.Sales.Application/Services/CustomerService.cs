using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;

        public CustomerService(
            ICustomerRepository customerRepository,
            IMapper mapper)
        {
            _customerRepository = customerRepository;
            _mapper = mapper;
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }

        public async Task<IEnumerable<CustomerDto>> ListCustomersAsync()
        {
            var customers = await _customerRepository.ListAsync();
            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }

        public async Task<PaginatedResult<CustomerDto>> ListCustomersPaginatedAsync(int pageNumber, int pageSize)
        {
            var paginatedCustomers = await _customerRepository.GetAllPaginatedAsync(pageNumber, pageSize);
            var customerDtos = _mapper.Map<IEnumerable<CustomerDto>>(paginatedCustomers.Items);
            return new PaginatedResult<CustomerDto>(customerDtos, paginatedCustomers.PageNumber, paginatedCustomers.PageSize, paginatedCustomers.TotalCount);
        }

        public async Task<CustomerDto> CreateCustomerAsync(CustomerDto dto)
        {
            var customer = _mapper.Map<Customer>(dto);
            customer.Id = Guid.NewGuid();
            await _customerRepository.AddAsync(customer);
            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> UpdateCustomerAsync(Guid id, CustomerDto dto)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
                throw new InvalidOperationException($"Customer with ID {id} not found.");

            _mapper.Map(dto, customer);
            await _customerRepository.UpdateAsync(customer);
            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task DeleteCustomerAsync(Guid id)
        {
            await _customerRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Query customers with filtering, sorting, and pagination
        /// </summary>
        public async Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec)
        {
            var result = await _customerRepository.QueryAsync(spec);
            var dtos = result.Items.Select(c => _mapper.Map<CustomerDto>(c)).ToList();
            return new PaginatedResult<CustomerDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
    }
}
