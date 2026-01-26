using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Domain.Constants;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;

namespace MyApp.Sales.Application.Services
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SalesOrderService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IServiceInvoker _serviceInvoker;

        public SalesOrderService(
            ISalesOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IMapper mapper,
            ILogger<SalesOrderService> logger,
            IEventPublisher eventPublisher,
            IServiceInvoker serviceInvoker)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _mapper = mapper;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _serviceInvoker = serviceInvoker;
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

        public async Task<SalesOrderDto> CreateQuoteAsync(CreateQuoteDto dto)
        {
            _logger.LogInformation(
                "Creating quote: OrderNumber={OrderNumber}, CustomerId={CustomerId}",
                dto.OrderNumber, dto.CustomerId);

            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException($"Customer with ID {dto.CustomerId} not found");

            // Check stock availability for all lines
            var stockChecks = await CheckStockAvailabilityAsync(dto.Lines);
            var unavailableItems = stockChecks.Where(s => !s.IsAvailable).ToList();
            
            if (unavailableItems.Any())
            {
                _logger.LogWarning(
                    "Quote creation - some items have insufficient stock: {UnavailableCount} items",
                    unavailableItems.Count);
                // Continue with quote creation but log the warning
            }

            // Create sales order as quote
            var quote = new SalesOrder(Guid.NewGuid())
            {
                OrderNumber = dto.OrderNumber,
                CustomerId = dto.CustomerId,
                OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
                Status = SalesOrderStatus.Draft,
                IsQuote = true,
                QuoteExpiryDate = DateTime.UtcNow.AddDays(dto.ValidityDays)
            };

            // Create order lines
            foreach (var lineDto in dto.Lines)
            {
                var line = new SalesOrderLine(Guid.NewGuid())
                {
                    SalesOrderId = quote.Id,
                    ProductId = lineDto.ProductId,
                    Quantity = lineDto.Quantity,
                    UnitPrice = lineDto.UnitPrice,
                    LineTotal = lineDto.Quantity * lineDto.UnitPrice
                };
                quote.Lines.Add(line);
            }

            quote.TotalAmount = quote.Lines.Sum(l => l.LineTotal);

            await _orderRepository.AddAsync(quote);

            // Publish SalesOrderCreatedEvent
            var salesOrderCreatedEvent = new SalesOrderCreatedEvent(
                quote.Id,
                quote.CustomerId,
                quote.OrderNumber,
                quote.IsQuote,
                quote.TotalAmount
            );

            try
            {
                await _eventPublisher.PublishAsync(MessagingConstants.Topics.SalesOrderCreated, salesOrderCreatedEvent);
                _logger.LogInformation("Published SalesOrderCreatedEvent for Quote {QuoteId}", quote.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish SalesOrderCreatedEvent for Quote {QuoteId}", quote.Id);
            }

            _logger.LogInformation(
                "Quote created successfully: QuoteId={QuoteId}, ExpiresAt={ExpiresAt}",
                quote.Id, quote.QuoteExpiryDate);

            return _mapper.Map<SalesOrderDto>(quote);
        }

        public async Task<SalesOrderDto> ConfirmQuoteAsync(ConfirmQuoteDto dto)
        {
            _logger.LogInformation("Confirming quote: QuoteId={QuoteId}", dto.QuoteId);

            var quote = await _orderRepository.GetByIdAsync(dto.QuoteId);
            if (quote == null)
            {
                throw new InvalidOperationException($"Quote {dto.QuoteId} not found");
            }

            if (!quote.IsQuote)
            {
                throw new InvalidOperationException($"Sales Order {dto.QuoteId} is not a quote");
            }

            if (quote.Status != SalesOrderStatus.Draft)
            {
                throw new InvalidOperationException($"Quote cannot be confirmed. Current status: {quote.Status}");
            }

            // Check if quote has expired
            if (quote.QuoteExpiryDate.HasValue && DateTime.UtcNow > quote.QuoteExpiryDate.Value)
            {
                throw new InvalidOperationException($"Quote has expired on {quote.QuoteExpiryDate.Value}");
            }

            // Check stock availability again before confirming
            var stockChecks = await CheckStockAvailabilityAsync(
                quote.Lines.Select(l => new CreateUpdateSalesOrderLineDto(l.ProductId, l.Quantity, l.UnitPrice)).ToList()
            );

            var unavailableItems = stockChecks.Where(s => !s.IsAvailable).ToList();
            if (unavailableItems.Any())
            {
                var itemsList = string.Join(", ", unavailableItems.Select(i => $"Product {i.ProductId}: requested {i.RequestedQuantity}, available {i.AvailableQuantity}"));
                throw new InvalidOperationException($"Insufficient stock for items: {itemsList}");
            }

            // Create fulfillment order via Orders service
            try
            {
                var createOrderRequest = new CreateOrderWithReservationDto
                {
                    OrderNumber = $"{quote.OrderNumber}-ORD",
                    Type = OrderType.Outbound,
                    SourceId = dto.WarehouseId,  // Shipping from warehouse
                    TargetId = quote.CustomerId, // Shipping to customer
                    ExternalOrderId = quote.Id,  // Link to SalesOrder
                    WarehouseId = dto.WarehouseId,
                    OrderDate = DateTime.UtcNow,
                    DestinationAddress = dto.ShippingAddress,
                    Lines = quote.Lines.Select(l => new CreateOrderLineDto
                    {
                        ProductId = l.ProductId,
                        Quantity = l.Quantity
                    }).ToList()
                };

                var fulfillmentOrder = await _serviceInvoker.InvokeAsync<CreateOrderWithReservationDto, OrderDto>(
                    ServiceNames.Orders,
                    ApiEndpoints.Orders.WithReservation,
                    HttpMethod.Post,
                    createOrderRequest);

                // Update quote
                quote.Status = SalesOrderStatus.Confirmed;
                quote.ConvertedToOrderId = fulfillmentOrder.Id;
                await _orderRepository.UpdateAsync(quote);

                // Publish SalesOrderConfirmedEvent
                var salesOrderConfirmedEvent = new SalesOrderConfirmedEvent(
                    quote.Id,
                    quote.ConvertedToOrderId.Value,
                    DateTime.UtcNow
                );

                try
                {
                    await _eventPublisher.PublishAsync(MessagingConstants.Topics.SalesOrderConfirmed, salesOrderConfirmedEvent);
                    _logger.LogInformation(
                        "Published SalesOrderConfirmedEvent for Quote {QuoteId}, Order {OrderId}",
                        quote.Id, quote.ConvertedToOrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish SalesOrderConfirmedEvent for Quote {QuoteId}", quote.Id);
                }

                _logger.LogInformation(
                    "Quote confirmed successfully: QuoteId={QuoteId}, OrderId={OrderId}",
                    quote.Id, quote.ConvertedToOrderId);

                return _mapper.Map<SalesOrderDto>(quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create fulfillment order for Quote {QuoteId}", dto.QuoteId);
                throw new InvalidOperationException($"Failed to create fulfillment order: {ex.Message}", ex);
            }
        }

        public async Task<List<StockAvailabilityCheckDto>> CheckStockAvailabilityAsync(List<CreateUpdateSalesOrderLineDto> lines)
        {
            _logger.LogInformation("Checking stock availability for {LineCount} items", lines.Count);

            var results = new List<StockAvailabilityCheckDto>();

            foreach (var line in lines)
            {
                try
                {
                    // Call Inventory service to check availability
                    var availability = await _serviceInvoker.InvokeAsync<StockAvailabilityDto>(
                        ServiceNames.Inventory,
                        $"{ApiEndpoints.Inventory.Availability}/{line.ProductId}",
                        HttpMethod.Get);

                    var totalAvailable = availability.TotalAvailable;
                    var warehouseStocks = availability.WarehouseStocks
                        .Select(ws => new WarehouseAvailabilityDto
                        {
                            WarehouseId = ws.WarehouseId,
                            WarehouseName = ws.WarehouseName ?? "Unknown",
                            AvailableQuantity = ws.AvailableQuantity
                        })
                        .ToList();

                    results.Add(new StockAvailabilityCheckDto
                    {
                        ProductId = line.ProductId,
                        RequestedQuantity = line.Quantity,
                        AvailableQuantity = totalAvailable,
                        IsAvailable = totalAvailable >= line.Quantity,
                        WarehouseStock = warehouseStocks
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check stock for Product {ProductId}", line.ProductId);
                    
                    // Return unavailable if check fails
                    results.Add(new StockAvailabilityCheckDto
                    {
                        ProductId = line.ProductId,
                        RequestedQuantity = line.Quantity,
                        AvailableQuantity = 0,
                        IsAvailable = false,
                        WarehouseStock = new List<WarehouseAvailabilityDto>()
                    });
                }
            }

            _logger.LogInformation(
                "Stock availability check complete: {AvailableCount}/{TotalCount} items available",
                results.Count(r => r.IsAvailable), results.Count);

            return results;
        }
    }
}
