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
    /// <summary>Implements customer management operations for the Sales service, including CRUD and event publishing.</summary>
    public class CustomerService : AppServiceBase, ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerService> _logger;

        /// <summary>Initializes a new <see cref="CustomerService"/> with the required dependencies.</summary>
        /// Initializes a new instance of the CustomerService class.
        /// <param name="customerRepository">The customer Repository.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="unitOfWork">The unit Of Work.</param>
        /// <param name="eventPublisher">The event Publisher.</param>
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

        /// <summary>Returns the customer with the given identifier, or <see langword="null"/> if not found.</summary>
        /// Gets the customer by id asynchronously.
        /// <param name="id">The id.</param>
        /// <returns>The matching <see cref="CustomerDto"/>, or <see langword="null"/>.</returns>
        public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }

        /// <summary>Returns the customer with the given name, or <see langword="null"/> if not found.</summary>
        /// Gets the customer by name asynchronously.
        /// <param name="name">The name.</param>
        /// <returns>The matching <see cref="CustomerDto"/>, or <see langword="null"/>.</returns>
        public async Task<CustomerDto?> GetCustomerByNameAsync(string name)
        {
            var customer = await _customerRepository.GetByNameAsync(name);
            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }

        /// <summary>Returns the customer with the given email address, or <see langword="null"/> if not found.</summary>
        /// Gets the customer by email asynchronously.
        /// <param name="email">The email.</param>
        /// <returns>The matching <see cref="CustomerDto"/>, or <see langword="null"/>.</returns>
        public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
        {
            var customer = await _customerRepository.GetByEmailAsync(email);
            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }

        /// <summary>Returns all customers as an enumerable sequence.</summary>
        /// Lists customers asynchronously.
        public async Task<IEnumerable<CustomerDto>> ListCustomersAsync()
        {
            var customers = await _customerRepository.ListAsync();
            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }

        /// <summary>Returns a paginated list of customers.</summary>
        /// Lists customers paginated asynchronously.
        /// <param name="pageNumber">The page Number.</param>
        /// <param name="pageSize">The page Size.</param>
        /// <returns>A <see cref="PaginatedResult{T}"/> containing the requested page of customers.</returns>
        public async Task<PaginatedResult<CustomerDto>> ListCustomersPaginatedAsync(int pageNumber, int pageSize)
        {
            var paginatedCustomers = await _customerRepository.GetAllPaginatedAsync(pageNumber, pageSize);
            var customerDtos = _mapper.Map<IEnumerable<CustomerDto>>(paginatedCustomers.Items);
            return new PaginatedResult<CustomerDto>(customerDtos, paginatedCustomers.PageNumber, paginatedCustomers.PageSize, paginatedCustomers.TotalCount);
        }

        /// <summary>Creates a new customer and publishes a <c>SalesCustomerCreatedEvent</c>.</summary>
        /// Creates a customer asynchronously.
        /// <param name="dto">The dto.</param>
        /// <returns>The created <see cref="CustomerDto"/>.</returns>
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

        /// <summary>Updates the customer with the given identifier and publishes a <c>SalesCustomerUpdatedEvent</c>.</summary>
        /// Updates the customer asynchronously.
        /// <param name="id">The id.</param>
        /// <param name="dto">The dto.</param>
        /// <returns>The updated <see cref="CustomerDto"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no customer with the given <paramref name="id"/> exists.</exception>
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

        /// <summary>Deletes the customer with the given identifier.</summary>
        /// Deletes the customer asynchronously.
        /// <param name="id">The id.</param>
        public async Task DeleteCustomerAsync(Guid id)
        {
            await _customerRepository.DeleteAsync(id);
            await SaveChangesAsync();
        }

        /// <summary>Returns a paginated list of customers that satisfy the given specification.</summary>
        /// Query customers asynchronously.
        /// <param name="spec">The spec.</param>
        /// <returns>A <see cref="PaginatedResult{T}"/> containing matching customers.</returns>
        public async Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec)
        {
            var result = await _customerRepository.QueryAsync(spec);
            var dtos = result.Items.Select(c => _mapper.Map<CustomerDto>(c)).ToList();
            return new PaginatedResult<CustomerDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
    }
}
