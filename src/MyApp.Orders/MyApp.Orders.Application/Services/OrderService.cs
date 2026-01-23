using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.BusinessRules;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Orders.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IOrderLineRepository _lines;
        private readonly IReservedStockRepository _reservedStockRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IServiceInvoker _serviceInvoker;

        public OrderService(
            IOrderRepository orders,
            IOrderLineRepository lines,
            IReservedStockRepository reservedStockRepository,
            IMapper mapper,
            ILogger<OrderService> logger,
            IEventPublisher eventPublisher,
            IServiceInvoker serviceInvoker)
        {
            _orders = orders;
            _lines = lines;
            _reservedStockRepository = reservedStockRepository;
            _mapper = mapper;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _serviceInvoker = serviceInvoker;
        }

        public async Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto)
        {
            var entity = _mapper.Map<Order>(dto);
            entity.Id = Guid.NewGuid();
            entity.OrderDate = dto.OrderDate;
            entity.Status = OrderStatus.Draft;

            foreach (var line in entity.Lines)
            {
                line.Id = Guid.NewGuid();
                line.OrderId = entity.Id;
            }

            await _orders.AddAsync(entity);

            return _mapper.Map<OrderDto>(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _orders.DeleteAsync(id);
        }

        public async Task<OrderDto> GetByIdAsync(Guid id)
        {
            var entity = await _orders.GetByIdAsync(id);
            return _mapper.Map<OrderDto>(entity!);
        }

        public async Task<IEnumerable<OrderDto>> ListAsync()
        {
            var list = await _orders.ListAsync();
            return list.Select(o => _mapper.Map<OrderDto>(o));
        }

        public async Task UpdateAsync(Guid id, CreateUpdateOrderDto dto)
        {
            var existing = await _orders.GetByIdAsync(id);
            if (existing == null) return;

            existing.OrderNumber = dto.OrderNumber;
            existing.OrderDate = dto.OrderDate;
            existing.Type = dto.Type;
            existing.SourceId = dto.SourceId;
            existing.TargetId = dto.TargetId;
            existing.ExternalOrderId = dto.ExternalOrderId;

            // Simple handling: replace lines
            existing.Lines.Clear();
            foreach (var l in dto.Lines)
            {
                var line = new OrderLine(Guid.NewGuid())
                {
                    OrderId = existing.Id,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity
                };
                existing.Lines.Add(line);
            }

            await _orders.UpdateAsync(existing);
        }

        public async Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto dto)
        {
            _logger.LogInformation(
                "Creating operational order with reservation: OrderNumber={OrderNumber}, Type={Type}, WarehouseId={WarehouseId}",
                dto.OrderNumber, dto.Type, dto.WarehouseId);

            // Validate order lines
            if (dto.Lines.Count == 0)
            {
                throw new InvalidOperationException("Order must have at least one line");
            }

            // Create order entity
            var order = new Order(Guid.NewGuid())
            {
                OrderNumber = dto.OrderNumber,
                Type = dto.Type,
                SourceId = dto.SourceId,
                TargetId = dto.TargetId,
                ExternalOrderId = dto.ExternalOrderId,
                OrderDate = dto.OrderDate,
                Status = OrderStatus.Draft,
                WarehouseId = dto.WarehouseId,
                DestinationAddress = dto.DestinationAddress
            };

            // Create order lines
            foreach (var lineDto in dto.Lines)
            {
                var line = new OrderLine(Guid.NewGuid())
                {
                    OrderId = order.Id,
                    ProductId = lineDto.ProductId,
                    Quantity = lineDto.Quantity
                };
                order.Lines.Add(line);
            }

            // Validate order using business rules
            if (order.Lines.Count == 0)
            {
                throw new InvalidOperationException("Order must have at least one line");
            }

            // Save order
            await _orders.AddAsync(order);

            // Reserve stock for each line via Dapr service invocation
            foreach (var line in order.Lines)
            {
                try
                {
                    _logger.LogInformation(
                        "Reserving stock for OrderLine: ProductId={ProductId}, Quantity={Quantity}, WarehouseId={WarehouseId}",
                        line.ProductId, line.Quantity, dto.WarehouseId);

                    // Call Inventory service to reserve stock
                    var reserveRequest = new
                    {
                        productId = line.ProductId,
                        warehouseId = dto.WarehouseId,
                        quantity = line.Quantity,
                        orderId = order.Id,
                        orderLineId = line.Id,
                        expiresAt = (DateTime?)null // Use default 24-hour expiry
                    };

                    var reservation = await _serviceInvoker.InvokeAsync<object, dynamic>(
                        "inventory",
                        "api/stockoperations/reserve",
                        HttpMethod.Post,
                        reserveRequest);

                    // Create ReservedStock record
                    var reservedStock = new ReservedStock(Guid.Parse(reservation.id.ToString()))
                    {
                        ProductId = line.ProductId,
                        WarehouseId = dto.WarehouseId,
                        OrderId = order.Id,
                        OrderLineId = line.Id,
                        Quantity = line.Quantity,
                        ReservedUntil = DateTime.Parse(reservation.reservedUntil.ToString()),
                        Status = ReservationStatus.Reserved
                    };

                    await _reservedStockRepository.AddAsync(reservedStock);

                    line.ReservedStockId = reservedStock.Id;
                    line.ReservedQuantity = line.Quantity;

                    _logger.LogInformation("Stock reserved successfully for OrderLine {OrderLineId}", line.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reserve stock for OrderLine {OrderLineId}, Product {ProductId}",
                        line.Id, line.ProductId);

                    // Roll back: cancel order and release any reservations made so far
                    await CancelOrderAsync(new CancelOrderDto
                    {
                        OrderId = order.Id,
                        Reason = $"Stock reservation failed: {ex.Message}"
                    });

                    throw new OrderFulfillmentException(order.Id, $"Failed to reserve stock: {ex.Message}");
                }
            }

            // Update order status to Approved (reservations confirmed)
            order.Status = OrderStatus.Approved;
            await _orders.UpdateAsync(order);

            // Publish OrderCreatedEvent
            var orderCreatedEvent = new OrderCreatedEvent(
                order.Id,
                order.OrderNumber,
                order.Type.ToString(),
                order.Lines.Select(l => new OrderLineEvent(l.ProductId, l.Quantity)).ToList()
            );

            try
            {
                await _eventPublisher.PublishAsync("orders.order.created", orderCreatedEvent);
                _logger.LogInformation("Published OrderCreatedEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreatedEvent for Order {OrderId}", order.Id);
            }

            _logger.LogInformation("Order created successfully with reservations: OrderId={OrderId}", order.Id);
            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> FulfillOrderAsync(FulfillOrderDto dto)
        {
            _logger.LogInformation("Fulfilling order: OrderId={OrderId}", dto.OrderId);

            var order = await _orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {dto.OrderId} not found");
            }

            if (order.Status != OrderStatus.Approved)
            {
                throw new OrderFulfillmentException(dto.OrderId, $"Order cannot be fulfilled. Current status: {order.Status}");
            }

            // Get reservations
            var reservations = await _reservedStockRepository.GetByOrderIdAsync(dto.OrderId);
            if (reservations.Count == 0)
            {
                throw new OrderFulfillmentException(dto.OrderId, "No stock reservations found for this order");
            }

            // Mark all reservations as fulfilled
            foreach (var reservation in reservations)
            {
                if (reservation.Status != ReservationStatus.Reserved)
                {
                    throw new OrderFulfillmentException(dto.OrderId,
                        $"Reservation {reservation.Id} is not in Reserved status: {reservation.Status}");
                }

                reservation.Status = ReservationStatus.Fulfilled;
                await _reservedStockRepository.UpdateAsync(reservation);
            }

            // Update order
            order.Status = OrderStatus.Completed;
            order.FulfilledDate = DateTime.UtcNow;
            order.WarehouseId = dto.WarehouseId;
            order.DestinationAddress = dto.ShippingAddress;
            order.TrackingNumber = dto.TrackingNumber;

            // Mark all lines as fulfilled
            foreach (var line in order.Lines)
            {
                line.IsFulfilled = true;
            }

            await _orders.UpdateAsync(order);

            // Publish OrderFulfilledEvent
            var orderFulfilledEvent = new OrderFulfilledEvent(
                order.Id,
                dto.WarehouseId,
                order.FulfilledDate.Value,
                dto.TrackingNumber
            );

            try
            {
                await _eventPublisher.PublishAsync("orders.order.fulfilled", orderFulfilledEvent);
                _logger.LogInformation("Published OrderFulfilledEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderFulfilledEvent for Order {OrderId}", order.Id);
            }

            _logger.LogInformation("Order fulfilled successfully: OrderId={OrderId}", order.Id);
            return _mapper.Map<OrderDto>(order);
        }

        public async Task CancelOrderAsync(CancelOrderDto dto)
        {
            _logger.LogInformation(
                "Cancelling order: {@CancelOrderData}",
                new { dto.OrderId, dto.Reason });

            var order = await _orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {dto.OrderId} not found");
            }

            if (order.Status == OrderStatus.Completed)
            {
                throw new InvalidOperationException($"Cannot cancel completed order {dto.OrderId}");
            }

            // Get and release all reservations
            var reservations = await _reservedStockRepository.GetByOrderIdAsync(dto.OrderId);
            foreach (var reservation in reservations)
            {
                if (reservation.Status == ReservationStatus.Reserved)
                {
                    // Call Inventory service to release reservation
                    try
                    {
                        await _serviceInvoker.InvokeAsync(
                            "inventory",
                            $"api/stockoperations/reservations/{reservation.Id}",
                            HttpMethod.Delete);

                        reservation.Status = ReservationStatus.Cancelled;
                        await _reservedStockRepository.UpdateAsync(reservation);

                        _logger.LogInformation("Released reservation {ReservationId} for Order {OrderId}",
                            reservation.Id, dto.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to release reservation {ReservationId}", reservation.Id);
                    }
                }
            }

            // Update order status
            order.Status = OrderStatus.Cancelled;
            await _orders.UpdateAsync(order);

            // Publish OrderCancelledEvent
            var orderCancelledEvent = new OrderCancelledEvent(
                order.Id,
                dto.Reason ?? string.Empty
            );

            try
            {
                await _eventPublisher.PublishAsync("orders.order.cancelled", orderCancelledEvent);
                _logger.LogInformation("Published OrderCancelledEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCancelledEvent for Order {OrderId}", order.Id);
            }

            _logger.LogInformation("Order cancelled successfully: OrderId={OrderId}", order.Id);
        }
    }
}
