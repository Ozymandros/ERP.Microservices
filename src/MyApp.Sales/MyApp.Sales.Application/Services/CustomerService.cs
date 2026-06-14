using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Services
{
    public class CustomerService : AppServiceBase, ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            IMapper mapper,
            ILogger<CustomerService> logger,
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher)
            : base(unitOfWork, eventPublisher, logger, ServiceNames.Sales)
        {
            _customerRepository = customerRepository;
            _mapper = mapper;
            _logger = logger;        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }

        public async Task<CustomerDto?> GetCustomerByNameAsync(string name)
        {
            var customer = await _customerRepository.GetByNameAsync(name);
            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }

        public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
        {
            var customer = await _customerRepository.GetByEmailAsync(email);
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
            await SaveChangesAsync();

            try
            {
                var @event = new SalesCustomerCreatedEvent(customer.Id, customer.Name, customer.Email);
                await EventPublisher.PublishAsync(MessagingConstants.Topics.SalesCustomerCreated, @event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish SalesCustomerCreatedEvent for Customer {CustomerId}", customer.Id);
            }

            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> UpdateCustomerAsync(Guid id, CreateUpdateCustomerDto dto)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
                throw new InvalidOperationException($"Customer with ID {id} not found.");

            _mapper.Map(dto, customer);
            await SaveChangesAsync();

            try
            {
                var @event = new SalesCustomerUpdatedEvent(customer.Id, customer.Name, customer.Email);
                await EventPublisher.PublishAsync(MessagingConstants.Topics.SalesCustomerUpdated, @event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish SalesCustomerUpdatedEvent for Customer {CustomerId}", customer.Id);
            }

            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task DeleteCustomerAsync(Guid id)
        {
            await _customerRepository.DeleteAsync(id);
            await SaveChangesAsync();
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
