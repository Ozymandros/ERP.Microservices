using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;

        public SalesOrderService(
            ISalesOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IMapper mapper)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _mapper = mapper;
        }

        public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            return order != null ? _mapper.Map<SalesOrderDto>(order) : null;
        }

        public async Task<IEnumerable<SalesOrderDto>> ListSalesOrdersAsync()
        {
            var orders = await _orderRepository.ListAsync();
            return _mapper.Map<IEnumerable<SalesOrderDto>>(orders);
        }

        public async Task<PaginatedResult<SalesOrderDto>> ListSalesOrdersPaginatedAsync(int pageNumber, int pageSize)
        {
            var paginatedOrders = await _orderRepository.GetAllPaginatedAsync(pageNumber, pageSize);
            var orderDtos = _mapper.Map<IEnumerable<SalesOrderDto>>(paginatedOrders.Items);
            return new PaginatedResult<SalesOrderDto>(orderDtos, paginatedOrders.PageNumber, paginatedOrders.PageSize, paginatedOrders.TotalCount);
        }

        public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateUpdateSalesOrderDto dto)
        {
            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException($"Customer with ID {dto.CustomerId} not found.");

            var order = _mapper.Map<SalesOrder>(dto);
            order.Id = Guid.NewGuid();
            order.OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate;

            // Calculate total from lines if provided
            if (dto.Lines.Any())
            {
                var lines = new List<SalesOrderLine>();
                foreach (var lineDto in dto.Lines)
                {
                    var line = _mapper.Map<SalesOrderLine>(lineDto);
                    line.Id = Guid.NewGuid();
                    line.SalesOrderId = order.Id;
                    line.LineTotal = line.Quantity * line.UnitPrice;
                    lines.Add(line);
                }
                order.Lines = lines;
                order.TotalAmount = lines.Sum(l => l.LineTotal);
            }

            await _orderRepository.AddAsync(order);
            return _mapper.Map<SalesOrderDto>(order);
        }

        public async Task<SalesOrderDto> UpdateSalesOrderAsync(Guid id, CreateUpdateSalesOrderDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"Order with ID {id} not found.");

            // Validate customer exists if changed
            if (order.CustomerId != dto.CustomerId)
            {
                var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
                if (customer == null)
                    throw new InvalidOperationException($"Customer with ID {dto.CustomerId} not found.");
            }

            // Update basic properties
            order.OrderNumber = dto.OrderNumber;
            order.CustomerId = dto.CustomerId;
            order.Status = (SalesOrderStatus)dto.Status;

            // Recalculate lines and total if provided
            if (dto.Lines.Any())
            {
                order.Lines.Clear();
                foreach (var lineDto in dto.Lines)
                {
                    var line = _mapper.Map<SalesOrderLine>(lineDto);
                    line.Id = Guid.NewGuid();
                    line.SalesOrderId = order.Id;
                    line.LineTotal = line.Quantity * line.UnitPrice;
                    order.Lines.Add(line);
                }
                order.TotalAmount = order.Lines.Sum(l => l.LineTotal);
            }

            await _orderRepository.UpdateAsync(order);
            return _mapper.Map<SalesOrderDto>(order);
        }

        public async Task DeleteSalesOrderAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Query sales orders with filtering, sorting, and pagination
        /// </summary>
        public async Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec)
        {
            var result = await _orderRepository.QueryAsync(spec);
            var dtos = result.Items.Select(so => _mapper.Map<SalesOrderDto>(so)).ToList();
            return new PaginatedResult<SalesOrderDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
    }
}
